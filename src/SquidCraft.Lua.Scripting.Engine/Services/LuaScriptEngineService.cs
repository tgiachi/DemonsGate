using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SquidCraft.Core.Attributes.Scripts;
using SquidCraft.Core.Data.Scripts;
using SquidCraft.Core.Directories;
using SquidCraft.Core.Enums;
using SquidCraft.Core.Extensions.Strings;
using SquidCraft.Core.Json;
using SquidCraft.Lua.Scripting.Engine.Data;
using SquidCraft.Lua.Scripting.Engine.Loaders;
using SquidCraft.Lua.Scripting.Engine.Utils;
using SquidCraft.Services.Data.Config.Sections;
using SquidCraft.Services.Data.Scripts;
using SquidCraft.Services.Interfaces;
using SquidCraft.Services.Types;
using DryIoc;
using MoonSharp.Interpreter;
using Serilog;

namespace SquidCraft.Lua.Scripting.Engine.Services;

/// <summary>
///     Lua engine service that integrates MoonSharp with the DemonsGate game engine
///     Provides script execution, module loading, and Lua meta file generation
/// </summary>
public class LuaScriptEngineService : IScriptEngineService, IDisposable
{
    private static readonly string[] collection = new[] { "log", "delay" };

    // Thread-safe collections
    private readonly ConcurrentDictionary<string, Action<object[]>> _callbacks = new();
    private readonly ConcurrentDictionary<string, object> _constants = new();

    private readonly DirectoriesConfig _directoriesConfig;
    private readonly List<string> _initScripts;
    private readonly ConcurrentDictionary<string, object> _loadedModules = new();
    private readonly ILogger _logger = Log.ForContext<LuaScriptEngineService>();

    // Script caching - using hash to avoid re-parsing identical scripts
    private readonly ConcurrentDictionary<string, string> _scriptCache = new();
    private readonly ScriptEngineConfig _scriptEngineConfig;
    private readonly List<ScriptModuleData> _scriptModules;
    private readonly IContainer _serviceProvider;
    private readonly IVersionService _versionService;
    private int _cacheHits;
    private int _cacheMisses;

    private bool _disposed;
    private bool _isInitialized;
    private Func<string, string> _nameResolver;

    public LuaScriptEngineService(
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

        CreateNameResolver();

        LuaScript = CreateOptimizedEngine();
    }

    public MoonSharp.Interpreter.Script LuaScript { get; }

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

            GC.SuppressFinalize(this);

            _logger.Debug("Lua engine disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error during Lua engine disposal");
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    ///     Event raised when a script error occurs
    /// </summary>
    public event EventHandler<ScriptErrorInfo>? OnScriptError;

    public object Engine => LuaScript;

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

            LuaScript.DoString(script);
            _logger.Debug("Script executed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (ScriptRuntimeException luaEx)
        {
            stopwatch.Stop();
            var errorInfo = CreateErrorInfo(luaEx, script);
            OnScriptError?.Invoke(this, errorInfo);

            _logger.Error(
                luaEx,
                "Lua error at line {Line}, column {Column}: {Message}",
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
        LuaScript.Globals[normalizedName] = value;

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
            var result = LuaScript.DoString($"return {command}");

            return ScriptResultBuilder.CreateSuccess().WithData(result.ToObject()).Build();
        }
        catch (ScriptRuntimeException luaEx)
        {
            var errorInfo = CreateErrorInfo(luaEx, command);
            OnScriptError?.Invoke(this, errorInfo);

            _logger.Error(
                luaEx,
                "Lua error at line {Line}, column {Column}: {Message}",
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
            AddConstant("engine", "DemonsGate");
            AddConstant("platform", Environment.OSVersion.Platform.ToString());

            _ = Task.Run(() => GenerateLuaMetaFileAsync(CancellationToken.None), CancellationToken.None);

            RegisterGlobalFunctions();

            ExecuteBootstrap();

            ExecuteBootFunction();
            _isInitialized = true;
            _logger.Information("Lua engine initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize Lua engine");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
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

    private void ExecuteBootFunction()
    {
        try
        {
            var onReadyFunc = LuaScript.Globals.Get("onReady");
            if (onReadyFunc.Type == DataType.Nil)
            {
                _logger.Warning("No onReady function defined in scripts");
                return;
            }

            LuaScript.Call(onReadyFunc);
            _logger.Debug("Boot function executed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing onReady function");
            throw;
        }
    }

    private MoonSharp.Interpreter.Script CreateOptimizedEngine()
    {
        var script = new MoonSharp.Interpreter.Script
        {
            Options =
            {
                // Configure MoonSharp options
                DebugPrint = s => _logger.Debug("[Lua] {Message}", s),
                ScriptLoader = new LuaScriptLoader(_directoriesConfig)
            }
        };

        _logger.Debug("Lua script loader configured for require() functionality");

        return script;
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

            // Register the type with MoonSharp
            UserData.RegisterType(module.ModuleType, InteropAccessMode.Reflection);

            // Create a table for the module
            var moduleTable = CreateModuleTable(instance, module.ModuleType);
            LuaScript.Globals[moduleName] = moduleTable;

            _loadedModules[moduleName] = instance;
        }

        RegisterEnums();
    }

    private Table CreateModuleTable(
        object instance,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        Type moduleType
    )
    {
        var moduleTable = new Table(LuaScript);

        var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<ScriptFunctionAttribute>() is not null);

        foreach (var method in methods)
        {
            var scriptFunctionAttr = method.GetCustomAttribute<ScriptFunctionAttribute>();
            if (scriptFunctionAttr is null)
            {
                continue;
            }

            var functionName = string.IsNullOrWhiteSpace(scriptFunctionAttr.FunctionName)
                ? _nameResolver(method.Name)
                : scriptFunctionAttr.FunctionName;

            // Create a closure that captures the instance and method
            var closure = CreateMethodClosure(instance, method);
            moduleTable[functionName] = closure;
        }

        return moduleTable;
    }

    private DynValue CreateMethodClosure(object instance, MethodInfo method)
    {
        return DynValue.NewCallback((context, args) =>
            {
                try
                {
                    var parameters = method.GetParameters();
                    var convertedArgs = new object[parameters.Length];

                    for (var i = 0; i < parameters.Length && i < args.Count; i++)
                    {
                        convertedArgs[i] = ConvertFromLua(args[i], parameters[i].ParameterType);
                    }

                    var result = method.Invoke(instance, convertedArgs);

                    if (method.ReturnType == typeof(void))
                    {
                        return DynValue.Nil;
                    }

                    return ConvertToLua(result);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error calling method {MethodName}", method.Name);
                    throw new ScriptRuntimeException(ex.Message);
                }
            }
        );
    }

    private static object? ConvertFromLua(DynValue dynValue, Type targetType)
    {
        return dynValue.Type switch
        {
            DataType.Nil     => null,
            DataType.Boolean => dynValue.Boolean,
            DataType.Number  => Convert.ChangeType(dynValue.Number, targetType, CultureInfo.InvariantCulture),
            DataType.String  => dynValue.String,
            DataType.Table   => dynValue.ToObject(),
            _                => dynValue.ToObject()
        };
    }

    private DynValue ConvertToLua(object? value)
    {
        if (value == null)
        {
            return DynValue.Nil;
        }

        return DynValue.FromObject(LuaScript, value);
    }

    [RequiresUnreferencedCode(
        "Enum metadata is discovered dynamically when building Lua documentation."
    )]
    private void RegisterEnums()
    {
        var enumsFound = LuaDocumentationGenerator.FoundEnums;

        foreach (var enumFound in enumsFound)
        {
            var enumName = _nameResolver(enumFound.Name);
            var enumTable = new Table(LuaScript);

            var names = Enum.GetNames(enumFound);
            var underlyingValues = Enum.GetValuesAsUnderlyingType(enumFound);

            for (var i = 0; i < names.Length; i++)
            {
                var rawValue = underlyingValues.GetValue(i);
                if (rawValue is null)
                {
                    continue;
                }

                var coercedValue = Convert.ToInt32(rawValue, CultureInfo.InvariantCulture);
                enumTable[names[i]] = coercedValue;
            }

            LuaScript.Globals[enumName] = enumTable;
            _logger.Debug("Registered enum {EnumName}", enumName);
        }
    }

    private void RegisterGlobalFunctions()
    {
        LuaScript.Globals["delay"] = (Func<int, Task>)(async milliseconds =>
        {
            await Task.Delay(Math.Min(milliseconds, 5000));
        });

        LuaScript.Globals["log"] = (Action<object>)(message => { _logger.Information("Lua: {Message}", message); });
    }

    [RequiresUnreferencedCode(
        "Lua meta generation relies on reflection-heavy LuaDocumentationGenerator which is not trim-safe."
    )]
    private async Task GenerateLuaMetaFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.Debug("Generating Lua meta files");

            var definitionDirectory = Path.Combine(_directoriesConfig.Root, _scriptEngineConfig.DefinitionPath);

            if (!Directory.Exists(definitionDirectory))
            {
                Directory.CreateDirectory(definitionDirectory);
            }

            // Generate meta.lua
            var documentation = LuaDocumentationGenerator.GenerateDocumentation(
                "DemonsGate",
                _versionService.GetVersionInfo().Version,
                _scriptModules,
                new Dictionary<string, object>(_constants),
                _nameResolver
            );

            var metaLuaPath = Path.Combine(definitionDirectory, "definitions.lua");
            await File.WriteAllTextAsync(metaLuaPath, documentation, cancellationToken);
            _logger.Debug("Lua meta file generated at {Path}", metaLuaPath);

            // Generate .luarc.json
            var luarcJson = GenerateLuarcJson();
            var luarcPath = Path.Combine(_directoriesConfig[DirectoryType.Scripts], ".luarc.json");
            await File.WriteAllTextAsync(luarcPath, luarcJson, cancellationToken);
            _logger.Debug("Lua configuration file generated at {Path}", luarcPath);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to generate Lua meta files");
        }
    }

    [RequiresUnreferencedCode("JSON serialization requires type metadata for script configuration.")]
    [RequiresDynamicCode("JSON serialization may generate dynamic code for script configuration.")]
    private string GenerateLuarcJson()
    {
        var globalsList = _constants.Keys.ToList();
        globalsList.AddRange(collection);

        var luarcConfig = new LuarcConfig
        {
            Runtime = new LuarcRuntimeConfig
            {
                Path =
                [
                    "?.lua",
                    "?/init.lua",
                    "modules/?.lua",
                    "modules/?/init.lua"
                ]
            },
            Workspace = new LuarcWorkspaceConfig
            {
                Library = [_scriptEngineConfig.DefinitionPath]
            },
            Diagnostics = new LuarcDiagnosticsConfig
            {
                Globals = [.. globalsList]
            }
        };

        return JsonUtils.Serialize(luarcConfig);
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

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _loadedModules.Clear();
        _callbacks.Clear();
        _constants.Clear();
        _isInitialized = false;

        _logger.Debug("Lua engine reset");
    }

    public (int ModuleCount, int CallbackCount, int ConstantCount, bool IsInitialized) GetStats()
    {
        return (_loadedModules.Count, _callbacks.Count, _constants.Count, _isInitialized);
    }

    /// <summary>
    ///     Creates detailed error information from a Lua exception
    /// </summary>
    private static ScriptErrorInfo CreateErrorInfo(ScriptRuntimeException luaEx, string sourceCode)
    {
        var errorInfo = new ScriptErrorInfo
        {
            Message = luaEx.DecoratedMessage ?? luaEx.Message,
            StackTrace = luaEx.StackTrace,
            LineNumber = 0,
            ColumnNumber = 0,
            ErrorType = "LuaError",
            SourceCode = sourceCode,
            FileName = "script.lua"
        };

        return errorInfo;
    }

    /// <summary>
    ///     Generates a hash for script caching
    /// </summary>
    private static string GetScriptHash(string script)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(script));
        return Convert.ToBase64String(hashBytes);
    }
}
