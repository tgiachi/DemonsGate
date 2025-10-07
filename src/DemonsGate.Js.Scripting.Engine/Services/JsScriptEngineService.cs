using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using DemonsGate.Core.Attributes.Scripts;
using DemonsGate.Core.Data.Scripts;
using DemonsGate.Core.Directories;
using DemonsGate.Core.Enums;
using DemonsGate.Core.Extensions.Strings;
using DemonsGate.Js.Scripting.Engine.Utils;
using DemonsGate.Services.Data.Config.Sections;
using DemonsGate.Services.Data.Scripts;
using DemonsGate.Services.Interfaces;
using DemonsGate.Services.Types;
using DryIoc;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Serilog;
using Spectra.Scripting.Utils;

namespace DemonsGate.Js.Scripting.Engine.Services;

/// <summary>
///     JavaScript engine service that integrates Jint with the Dust and Rune game engine
///     Provides script execution, module loading, and TypeScript definition generation
/// </summary>
public class JsScriptEngineService : IScriptEngineService, IDisposable
{
    // Thread-safe collections
    private readonly ConcurrentDictionary<string, Action<object[]>> _callbacks = new();
    private readonly ConcurrentDictionary<string, object> _constants = new();
    private readonly ConcurrentDictionary<string, JsValue> _loadedModules = new();

    // Script caching - using hash to avoid re-parsing identical scripts
    private readonly ConcurrentDictionary<string, string> _scriptCache = new();
    private int _cacheHits;
    private int _cacheMisses;

    private readonly DirectoriesConfig _directoriesConfig;
    private readonly List<string> _initScripts;
    private readonly ILogger _logger = Log.ForContext<JsScriptEngineService>();
    private readonly ScriptEngineConfig _scriptEngineConfig;
    private readonly List<ScriptModuleData> _scriptModules;
    private readonly IContainer _serviceProvider;
    private readonly IVersionService _versionService;
    private readonly SourceMapResolver? _sourceMapResolver;

    private bool _disposed;
    private bool _isInitialized;
    private Func<string, string> _nameResolver;

    /// <summary>
    ///     Event raised when a script error occurs
    /// </summary>
    public event EventHandler<ScriptErrorInfo>? OnScriptError;

    public JsScriptEngineService(
        DirectoriesConfig directoriesConfig,
        ScriptEngineConfig scriptEngineConfig,
        List<ScriptModuleData> scriptModules,
        IVersionService versionService,
        IContainer serviceProvider
    )
    {
        ArgumentNullException.ThrowIfNull(directoriesConfig);
        ArgumentNullException.ThrowIfNull(scriptEngineConfig);
        ArgumentNullException.ThrowIfNull(scriptModules);
        ArgumentNullException.ThrowIfNull(versionService);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _scriptModules = scriptModules;
        _directoriesConfig = directoriesConfig;
        _scriptEngineConfig = scriptEngineConfig;
        _versionService = versionService;
        _serviceProvider = serviceProvider;
        _initScripts = _scriptEngineConfig.InitScriptsFileNames;

        // Initialize source map resolver if enabled
        if (_scriptEngineConfig.EnableSourceMaps)
        {
            var sourceMapsPath = Path.Combine(_directoriesConfig.Root, _scriptEngineConfig.SourceMapsPath);
            _sourceMapResolver = new SourceMapResolver(sourceMapsPath);
        }

        CreateNameResolver();

        JsEngine = CreateOptimizedEngine();
    }

    public Jint.Engine JsEngine { get; }

    public object Engine => JsEngine;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _loadedModules.Clear();
            _callbacks.Clear();
            _constants.Clear();

            JsEngine.Dispose();
            GC.SuppressFinalize(this);

            _logger.Debug("JavaScript engine disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error during JavaScript engine disposal");
        }
        finally
        {
            _disposed = true;
        }
    }

    public void AddInitScript(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            throw new ArgumentException("Script cannot be null or empty", nameof(script));
        }

        _initScripts.Add(script);
    }

    public void ExecuteScript(string script)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(script);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Try to use cached script if caching is enabled
            if (_scriptEngineConfig.EnableScriptCaching)
            {
                var scriptHash = GetScriptHash(script);
                if (_scriptCache.ContainsKey(scriptHash))
                {
                    Interlocked.Increment(ref _cacheHits);
                    _logger.Debug("Script found in cache");
                }
                else
                {
                    Interlocked.Increment(ref _cacheMisses);
                    _scriptCache.TryAdd(scriptHash, script);
                }
            }

            JsEngine.Execute(script);
            _logger.Debug("Script executed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (JavaScriptException jsEx)
        {
            stopwatch.Stop();
            var errorInfo = CreateErrorInfo(jsEx, script);
            OnScriptError?.Invoke(this, errorInfo);

            _logger.Error(
                jsEx,
                "JavaScript error at line {Line}, column {Column}: {Message}",
                errorInfo.LineNumber,
                errorInfo.ColumnNumber,
                errorInfo.Message
            );
            throw;
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            _logger.Error(
                e,
                "Error executing script: {ScriptPreview}",
                script.Length > 100 ? script[..100] + "..." : script
            );
            throw;
        }
    }

    public void ExecuteScriptFile(string scriptFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptFile);

        if (!File.Exists(scriptFile))
        {
            throw new FileNotFoundException($"Script file not found: {scriptFile}", scriptFile);
        }

        try
        {
            var content = File.ReadAllText(scriptFile);
            _logger.Debug("Executing script file: {FileName}", Path.GetFileName(scriptFile));
            ExecuteScript(content);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to execute script file: {FileName}", Path.GetFileName(scriptFile));
            throw;
        }
    }

    public void AddCallback(string name, Action<object[]> callback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(callback);

        var normalizedName = name.ToSnakeCaseUpper();
        _callbacks[normalizedName] = callback;

        _logger.Debug("Callback registered: {Name}", normalizedName);
    }

    public void AddConstant(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.ToSnakeCaseUpper();

        if (_constants.ContainsKey(normalizedName))
        {
            _logger.Warning("Constant {Name} already exists, overwriting", normalizedName);
        }

        _constants[normalizedName] = value;
        JsEngine.SetValue(normalizedName, value);

        _logger.Debug("Constant added: {Name}", normalizedName);
    }

    public void ExecuteCallback(string name, params object[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.ToSnakeCaseUpper();

        if (_callbacks.TryGetValue(normalizedName, out var callback))
        {
            try
            {
                _logger.Debug("Executing callback {Name}", normalizedName);
                callback(args);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error executing callback {Name}", normalizedName);
                throw;
            }
        }
        else
        {
            _logger.Warning("Callback {Name} not found", normalizedName);
        }
    }

    public void AddScriptModule(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        _scriptModules.Add(new ScriptModuleData(type));
    }

    public string ToScriptEngineFunctionName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _nameResolver(name);
    }

    public ScriptResult ExecuteFunction(string command)
    {
        try
        {
            var result = JsEngine.Evaluate(command);

            return ScriptResultBuilder.CreateSuccess().WithData(result.ToObject()).Build();
        }
        catch (JavaScriptException jsEx)
        {
            var errorInfo = CreateErrorInfo(jsEx, command);
            OnScriptError?.Invoke(this, errorInfo);

            _logger.Error(
                jsEx,
                "JavaScript error at line {Line}, column {Column}: {Message}",
                errorInfo.LineNumber,
                errorInfo.ColumnNumber,
                errorInfo.Message
            );

            return ScriptResultBuilder.CreateError()
                .WithMessage($"{errorInfo.ErrorType}: {errorInfo.Message} at line {errorInfo.LineNumber}")
                .Build();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to execute function: {Command}", command);

            return ScriptResultBuilder.CreateError().WithMessage(ex.Message).Build();
        }
    }

    public async Task<ScriptResult> ExecuteFunctionAsync(string command)
    {
        return ExecuteFunction(command);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            _logger.Warning("Script engine is already initialized");
            return;
        }

        try
        {
            await RegisterScriptModulesAsync(CancellationToken.None);

            AddConstant("version", _versionService.GetVersionInfo().Version);
            AddConstant("engine", "Spectra");
            AddConstant("platform", Environment.OSVersion.Platform.ToString());

            _ = Task.Run(() => GenerateTypeScriptDefinitionsAsync(CancellationToken.None), CancellationToken.None);

            RegisterGlobalFunctions();


            ExecuteBootstrap();

            ExecuteBootFunction();
            _isInitialized = true;
            _logger.Information("JavaScript engine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize JavaScript engine");
            throw;
        }
    }

    private void ExecuteBootFunction()
    {
        if (JsEngine.GetValue("onReady").IsUndefined())
        {
            _logger.Warning("No onReady function defined in scripts");
            return;
        }

        try
        {
            JsEngine.Invoke("onReady");
            _logger.Debug("Boot function executed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing onReady function");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private Jint.Engine CreateOptimizedEngine()
    {
        var typeResolver = TypeResolver.Default;
        typeResolver.MemberNameCreator = MemberNameCreator;

        return new Jint.Engine(options =>
            {
                options.EnableModules(_directoriesConfig[DirectoryType.Scripts]);

                options.AllowClr(GetType().Assembly);

                options.SetTypeResolver(typeResolver);

                options.Strict();

                // Use configurable limits
                options.LimitMemory(_scriptEngineConfig.MaxMemoryBytes);

                options.TimeoutInterval(TimeSpan.FromSeconds(_scriptEngineConfig.TimeoutSeconds));

                options.MaxStatements(_scriptEngineConfig.MaxStatements);

                options.DebugMode(_scriptEngineConfig.EnableDebugMode);

                options.Culture(CultureInfo.InvariantCulture);
            }
        );
    }

    private void CreateNameResolver()
    {
        _nameResolver = name => name.ToSnakeCase();

        _nameResolver = _scriptEngineConfig.ScriptNameConversion switch
        {
            ScriptNameConversion.CamelCase  => name => name.ToCamelCase(),
            ScriptNameConversion.PascalCase => name => name.ToPascalCase(),
            ScriptNameConversion.SnakeCase  => name => name.ToSnakeCase(),
            _                               => _nameResolver
        };
    }

    private IEnumerable<string> MemberNameCreator(MemberInfo memberInfo)
    {
        var memberType = _nameResolver(memberInfo.Name);
        _logger.Verbose("[JS] Creating member name  {MemberInfo}", memberType);
        yield return memberType;
    }


    private void ExecuteBootstrap()
    {
        foreach (var file in _initScripts.Select(s => Path.Combine(_directoriesConfig[DirectoryType.Scripts], s)))
        {
            if (File.Exists(file))
            {
                var fileName = Path.GetFileName(file);
                _logger.Information("Executing {FileName} script", fileName);
                ExecuteScriptFile(file);
            }
        }
    }

    private async Task RegisterScriptModulesAsync(CancellationToken cancellationToken)
    {
        foreach (var module in _scriptModules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scriptModuleAttribute = module.ModuleType.GetCustomAttribute<ScriptModuleAttribute>();
            if (scriptModuleAttribute is null)
            {
                continue;
            }

            if (!_serviceProvider.IsRegistered(module.ModuleType))
            {
                _serviceProvider.Register(module.ModuleType, Reuse.Singleton);
            }

            var instance = _serviceProvider.GetService(module.ModuleType);
            if (instance is null)
            {
                throw new InvalidOperationException(
                    $"Unable to create instance of script module {module.ModuleType.Name}"
                );
            }

            var moduleName = scriptModuleAttribute.Name;
            _logger.Debug("Registering script module {Name}", moduleName);

            _loadedModules[moduleName] = JsValue.FromObject(JsEngine, instance);
            JsEngine.SetValue(moduleName, instance);
        }

        RegisterEnums();
    }

    private void RegisterEnums()
    {
        var enumsFound = TypeScriptDocumentationGenerator.FoundEnums;

        foreach (var enumFound in enumsFound)
        {
            var enumName = _nameResolver(enumFound.Name);
            JsEngine.SetValue(enumName, TypeReference.CreateTypeReference(JsEngine, enumFound));
            _logger.Debug("Registered enum {EnumName}", enumName);
        }
    }

    private void RegisterGlobalFunctions()
    {
        JsEngine.SetValue(
            "delay",
            new Func<int, Task>(async milliseconds => { await Task.Delay(Math.Min(milliseconds, 5000)); })
        );

        JsEngine.SetValue("log", new Action<object>(message => { _logger.Information("JS: {Message}", message); }));
    }

    private async Task GenerateTypeScriptDefinitionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Debug("Generating TypeScript definitions");

            var documentation = TypeScriptDocumentationGenerator.GenerateDocumentation(
                "Spectra",
                _versionService.GetVersionInfo().Version,
                _scriptModules,
                new Dictionary<string, object>(_constants),
                _nameResolver
            );

            var definitionPath = Path.Combine(
                _directoriesConfig.Root,
                _scriptEngineConfig.DefinitionPath,
                "index.d.ts"
            );

            var directory = Path.GetDirectoryName(definitionPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(definitionPath, documentation, cancellationToken);
            _logger.Debug("TypeScript definitions generated at {Path}", definitionPath);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to generate TypeScript definitions");
        }
    }


    public async Task ExecuteScriptFileAsync(string scriptFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptFile);

        if (!File.Exists(scriptFile))
        {
            throw new FileNotFoundException($"Script file not found: {scriptFile}", scriptFile);
        }

        try
        {
            var content = await File.ReadAllTextAsync(scriptFile).ConfigureAwait(false);
            _logger.Debug("Executing script file asynchronously: {FileName}", Path.GetFileName(scriptFile));
            ExecuteScript(content);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to execute script file asynchronously: {FileName}", Path.GetFileName(scriptFile));
            throw;
        }
    }

    public JsValue EvaluateExpression(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        try
        {
            var result = JsEngine.Evaluate(expression);
            _logger.Debug("Expression evaluated: {Expression}", expression);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to evaluate expression: {Expression}", expression);
            throw;
        }
    }

    public bool IsGlobalDefined(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            var value = JsEngine.GetValue(name);
            return !value.IsUndefined();
        }
        catch
        {
            return false;
        }
    }

    public JsValue GetGlobalValue(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            return JsEngine.GetValue(name);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to get global value: {Name}", name);
            throw;
        }
    }

    public void SetGlobalValue(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        try
        {
            JsEngine.SetValue(name, value);
            _logger.Debug("Global value set: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set global value: {Name}", name);
            throw;
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _loadedModules.Clear();
        _callbacks.Clear();
        _constants.Clear();
        _isInitialized = false;

        _logger.Debug("JavaScript engine reset");
    }

    public (int ModuleCount, int CallbackCount, int ConstantCount, bool IsInitialized) GetStats()
    {
        return (_loadedModules.Count, _callbacks.Count, _constants.Count, _isInitialized);
    }

    /// <summary>
    ///     Gets execution metrics for performance monitoring
    /// </summary>
    public ScriptExecutionMetrics GetExecutionMetrics()
    {
        return new ScriptExecutionMetrics
        {
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            TotalScriptsCached = _scriptCache.Count
        };
    }

    /// <summary>
    ///     Clears the script cache
    /// </summary>
    public void ClearScriptCache()
    {
        _scriptCache.Clear();
        _cacheHits = 0;
        _cacheMisses = 0;
        _logger.Information("Script cache cleared");
    }

    /// <summary>
    ///     Creates detailed error information from a JavaScript exception
    /// </summary>
    private ScriptErrorInfo CreateErrorInfo(JavaScriptException jsEx, string sourceCode)
    {
        var errorInfo = new ScriptErrorInfo
        {
            Message = jsEx.Message,
            StackTrace = jsEx.StackTrace,
            LineNumber = jsEx.Location.Start.Line,
            ColumnNumber = jsEx.Location.Start.Column,
            ErrorType = jsEx.Error?.ToString() ?? "Error",
            SourceCode = sourceCode,
            FileName = "script.js"
        };

        // Source maps not fully implemented yet (SourceLocation doesn't have Source property in this Jint version)
        // Will be enhanced when upgrading Jint to a version that supports it

        return errorInfo;
    }

    /// <summary>
    ///     Generates a hash for script caching
    /// </summary>
    private static string GetScriptHash(string script)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(script));
        return Convert.ToBase64String(hashBytes);
    }
}
