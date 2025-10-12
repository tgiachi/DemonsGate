using System.Globalization;
using SquidCraft.Core.Data.Internal;
using SquidCraft.Core.Directories;
using SquidCraft.Core.Enums;
using SquidCraft.Core.Extensions.Directories;
using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Core.Json;
using SquidCraft.Services.Data.Config;
using SquidCraft.Services.Data.Config.Options;
using SquidCraft.Services.Events.Engine;
using SquidCraft.Services.Extensions.Loggers;
using SquidCraft.Services.Interfaces;
using SquidCraft.Services.Types;
using DryIoc;
using Serilog;
using Serilog.Formatting.Compact;

namespace SquidCraft.Server;

// ##REFORMAT##
/// <summary>
/// Handles the initialization and lifecycle of the SquidCraft server.
/// </summary>
public class SquidCraftBootstrap : IDisposable
{
    private readonly SquidCraftServerOptions _options;

    private readonly Container _container;

    private Func<IContainer, IContainer> _registerServicesCallback;

    private DirectoriesConfig _directoriesConfig;


    public SquidCraftBootstrap(SquidCraftServerOptions options)
    {
        _options = options;

        _container = new Container(Rules.Default.WithUseInterpretation());

        InitializeDirectories();
        InitializeLogger();
        LoadConfig();
    }


    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Log.Information("Starting Squid Craft Server...");

        _registerServicesCallback?.Invoke(_container);

        await StartServicesAsync(cancellationToken);

        if (_options.IsShellEnabled)
        {
            HookShellCommands(cancellationToken);
        }

        await _container.Resolve<IEventBusService>().PublishAsync(new EngineStartedEvent(), cancellationToken);

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Shutdown signal received.");
        }

        await _container.Resolve<IEventBusService>().PublishAsync(new EngineStoppingEvent(), cancellationToken);

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
                    Console.Write("SQUIDC> ");
                    var input = Console.ReadLine();
                    if (input == null)
                    {
                        continue;
                    }

                    var command = input.Trim().ToLowerInvariant();

                    var commandService = _container.Resolve<ICommandService>();

                    try
                    {
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
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Unhandled Exception: {ex.Message}");
                    }


                    Console.ResetColor();
                }
            },
            cancellationToken
        );
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        Log.Information("Stopping SquidCraft Server...");
        await StopServicesAsync(cancellationToken);
        Log.Information("SquidCraft Server stopped.");
        await Log.CloseAndFlushAsync();
    }

    private void InitializeDirectories()
    {
        if (Environment.GetEnvironmentVariable("SQUIDCRAFT_SERVER_ROOT") != null)
        {
            _options.RootDirectory = Environment.GetEnvironmentVariable("SQUIDCRAFT_SERVER_ROOT");
        }

        if (string.IsNullOrEmpty(_options.RootDirectory))
        {
            _options.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "squidcraft_root");
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
                path: Path.Combine(_directoriesConfig[DirectoryType.Logs], "squidcraft-.log"),
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
            var serviceInstance = _container.Resolve(serviceDefinition.ServiceType) as ISquidCraftService;

            if (serviceInstance is ISquidCraftStartableService startableService)
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
            var serviceInstance = _container.Resolve(serviceDefinition.ServiceType) as ISquidCraftService;

            if (serviceInstance is ISquidCraftStartableService startableService)
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
                JsonUtils.Serialize(new SquidCraftServerConfig())
            );
        }

        var config = JsonUtils.Deserialize<SquidCraftServerConfig>(
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
