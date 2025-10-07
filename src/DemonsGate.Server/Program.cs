using System.Text;
using ConsoleAppFramework;
using DemonsGate.Core.Utils;
using DemonsGate.Server;
using DemonsGate.Services.Data.Config;
using DemonsGate.Services.Types;

await ConsoleApp.RunAsync(
    args,
    async (
        CancellationTokenRegistration ct = new CancellationTokenRegistration(),
        bool showHeader = true,
        string pidFileName = "demonsgame.pid",
        LogLevelType logLevel = LogLevelType.Information,
        string rootDirectory = "."
    ) =>
    {
        var options = new DemonsGateServerOptions()
        {
            LogLevel = logLevel,
            PidFileName = pidFileName,
            RootDirectory = rootDirectory
        };

        if (showHeader)
        {
            var headerContext = ResourceUtils.GetEmbeddedResourceContent("Assets.header.txt", typeof(Program).Assembly);

            Console.WriteLine(Encoding.UTF8.GetString(headerContext));
        }

        var bootstrap = new DemonsGateBootstrap(options, ct.Token);

        await bootstrap.RunAsync();

    }
);
