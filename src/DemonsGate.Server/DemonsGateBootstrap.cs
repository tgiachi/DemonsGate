using System.Globalization;
using DemonsGate.Core.Data.Internal;
using DemonsGate.Core.Directories;
using DemonsGate.Core.Enums;
using DemonsGate.Core.Extensions.Directories;
using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Core.Json;
using DemonsGate.Services.Data.Config;
using DemonsGate.Services.Data.Config.Options;
using DemonsGate.Services.Extensions.Loggers;
using DemonsGate.Services.Interfaces;
using DemonsGate.Services.Types;
using DryIoc;
using Serilog;
using Serilog.Formatting.Compact;

namespace DemonsGate.Server;

// ##REFORMAT##
/// <summary>
/// Handles the initialization and lifecycle of the DemonsGate server.
/// </summary>
public class DemonsGateBootstrap : IDisposable
{
    private readonly DemonsGateServerOptions _options;

    private readonly IContainer _container;

    private Func<IContainer, IContainer> _registerServicesCallback;

    private DirectoriesConfig _directoriesConfig;


    public DemonsGateBootstrap(DemonsGateServerOptions options)
    {
        _options = options;

        _container = new Container(Rules.Default.WithUseInterpretation());

        InitializeDirectories();
        InitializeLogger();
        LoadConfig();
    }


    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Log.Information("Starting DemonsGate Server...");

        _registerServicesCallback?.Invoke(_container);

        await StartServicesAsync(cancellationToken);

        if (_options.IsShellEnabled)
        {
            HookShellCommands(cancellationToken);
        }

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Shutdown signal received.");
        }

        await StopAsync(cancellationToken);
    }

    private void HookShellCommands(CancellationToken cancellationToken)
    {
        Task.Run(
            async () =>
            {
                Log.Debug("Hooking Shell Commands");

                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.Write("DEMONS> ");
                    var input = Console.ReadLine();
                    if (input == null)
                    {
                        continue;
                    }

                    var command = input.Trim().ToLowerInvariant();

                    var commandService = _container.Resolve<ICommandService>();

                    var commandResult = await commandService.ExecuteCommandAsync(command, CommandSourceType.Console, -1);

                    if (!commandResult.Success)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error: {commandResult.Exception?.Message}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(commandResult.Output);
                    }

                    Console.ResetColor();
                }
            },
            cancellationToken
        );
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping DemonsGate Server...");
        await StopServicesAsync(cancellationToken);
        Log.Information("DemonsGate Server stopped.");
        await Log.CloseAndFlushAsync();
    }

    private void InitializeDirectories()
    {
        if (Environment.GetEnvironmentVariable("DEMONSGATE_SERVER_ROOT") != null)
        {
            _options.RootDirectory = Environment.GetEnvironmentVariable("DEMONSGATE_SERVER_ROOT");
        }

        if (string.IsNullOrEmpty(_options.RootDirectory))
        {
            _options.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "demonsgate_root");
        }

        _options.RootDirectory = _options.RootDirectory.ResolvePathAndEnvs();

        _directoriesConfig = new DirectoriesConfig(_options.RootDirectory, Enum.GetNames<DirectoryType>());

        _container.RegisterInstance(_directoriesConfig);
    }

    private void InitializeLogger()
    {
        var loggingConfig = new LoggerConfiguration()
            .MinimumLevel
            .Is(_options.LogLevel.ToSerilogLogLevel())
            .WriteTo.Console(formatProvider: CultureInfo.CurrentCulture)
            .WriteTo.File(
                formatter: new CompactJsonFormatter(),
                path: Path.Combine(_directoriesConfig[DirectoryType.Logs], "demonsgate-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7
            );

        Log.Logger = loggingConfig.CreateLogger();

        Log.Logger.Information("Root Directory: {RootDirectory}", _directoriesConfig.Root);
        Log.Logger.Information("Log Level: {LogLevel}", _options.LogLevel);
    }

    public void RegisterServices(Func<IContainer, IContainer> registerServices)
    {
        _registerServicesCallback = registerServices;
    }

    private async Task StartServicesAsync(CancellationToken cancellationToken)
    {
        var servicesToStart = _container.Resolve<List<ServiceDefinitionObject>>().OrderBy(s => s.Priority);

        foreach (var serviceDefinition in servicesToStart)
        {
            Log.Logger.Debug("Initializing service: {ServiceName}", serviceDefinition.ServiceType.Name);
            var serviceInstance = _container.Resolve(serviceDefinition.ServiceType) as IDemonsGateService;

            if (serviceInstance is IDemonsGateStartableService startableService)
            {
                Log.Information("Starting service: {ServiceName}", serviceDefinition.ServiceType.Name);
                await startableService.StartAsync(cancellationToken);
                Log.Information("Service started: {ServiceName}", serviceDefinition.ServiceType.Name);
            }
        }
    }

    private async Task StopServicesAsync(CancellationToken cancellationToken)
    {
        var servicesToStop = _container.Resolve<List<ServiceDefinitionObject>>().OrderByDescending(s => s.Priority);

        foreach (var serviceDefinition in servicesToStop)
        {
            Log.Logger.Debug("Stopping service: {ServiceName}", serviceDefinition.ServiceType.Name);
            var serviceInstance = _container.Resolve(serviceDefinition.ServiceType) as IDemonsGateService;

            if (serviceInstance is IDemonsGateStartableService startableService)
            {
                Log.Information("Stopping service: {ServiceName}", serviceDefinition.ServiceType.Name);
                await startableService.StopAsync(cancellationToken);
                Log.Information("Service stopped: {ServiceName}", serviceDefinition.ServiceType.Name);
            }
        }
    }

    private void LoadConfig()
    {
        var configFileName = Path.Combine(_options.RootDirectory, _options.ConfigFileName);
        if (!File.Exists(configFileName))
        {
            File.WriteAllText(
                configFileName,
                JsonUtils.Serialize(new DemonsGateServerConfig())
            );
        }

        var config = JsonUtils.Deserialize<DemonsGateServerConfig>(
            File.ReadAllText(configFileName)
        );

        _container.RegisterInstance(config);
        _container.RegisterInstance(config.EventLoop);
        _container.RegisterInstance(config.Network);
        _container.RegisterInstance(config.ScriptEngine);
        _container.RegisterInstance(config.Diagnostic);

        Log.Information("Configuration loaded from {ConfigFileName}", configFileName);
    }

    public void Dispose()
    {
        _container.Dispose();
        GC.SuppressFinalize(this);
    }
}
