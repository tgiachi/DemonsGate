using System.Globalization;
using DemonsGate.Core.Directories;
using DemonsGate.Core.Enums;
using DemonsGate.Services.Data.Config;
using DemonsGate.Services.Extensions.Loggers;
using DryIoc;
using Serilog;
using Serilog.Formatting.Compact;

namespace DemonsGate.Server;

public class DemonsGateBootstrap
{
    private readonly DemonsGateServerOptions _options;

    private readonly CancellationToken _terminationToken;

    private readonly IContainer _container;

    private DirectoriesConfig _directoriesConfig;

    public DemonsGateBootstrap(DemonsGateServerOptions options, CancellationToken terminationToken)
    {
        _options = options;
        _terminationToken = terminationToken;
        _container = new Container(Rules.Default.WithUseInterpretation());

        InitializeDirectories();
        InitializeLogger();
    }


    public async Task RunAsync()
    {
        while (!_terminationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, _terminationToken);
            Console.WriteLine("Running...{0}", DateTime.Now);
        }

        await StopAsync();
    }

    public async Task StopAsync()
    {
        Console.WriteLine("Stopping...");
    }

    private void InitializeDirectories()
    {
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
    }
}
