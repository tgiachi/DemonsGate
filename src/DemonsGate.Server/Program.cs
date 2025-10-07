using System.Text;
using ConsoleAppFramework;
using DemonsGate.Core.Extensions.Services;
using DemonsGate.Core.Interfaces.EventLoop;
using DemonsGate.Core.Json;
using DemonsGate.Core.Utils;
using DemonsGate.Js.Scripting.Engine.Extensions.Scripts;
using DemonsGate.Js.Scripting.Engine.Modules;
using DemonsGate.Js.Scripting.Engine.Services;
using DemonsGate.Network.Extensions;
using DemonsGate.Network.Messages;
using DemonsGate.Server;
using DemonsGate.Services.Context;
using DemonsGate.Services.Data.Config.Options;
using DemonsGate.Services.Impl;
using DemonsGate.Services.Interfaces;
using DemonsGate.Services.Types;
using DryIoc;

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await ConsoleApp.RunAsync(
    args,
    async (
        ConsoleAppContext context,
        bool showHeader = true,
        string pidFileName = "demonsgame.pid",
        LogLevelType logLevel = LogLevelType.Information,
        string? rootDirectory = null,
        string configFileName = "demonsgate_server.json"
    ) =>
    {


        JsonUtils.RegisterJsonContext(DemonsGateJsonContext.Default);

        var options = new DemonsGateServerOptions()
        {
            LogLevel = logLevel,
            PidFileName = pidFileName,
            RootDirectory = rootDirectory,
            ConfigFileName = configFileName
        };

        if (showHeader)
        {
            var headerContext = ResourceUtils.GetEmbeddedResourceContent("Assets.header.txt", typeof(Program).Assembly);

            Console.WriteLine(Encoding.UTF8.GetString(headerContext));
        }

        var bootstrap = new DemonsGateBootstrap(options);

        bootstrap.RegisterServices(container =>
            {
                container
                    .AddService<IEventBusService, EventBusService>()
                    .AddService<IVersionService, VersionService>()
                    .AddService<IScriptEngineService, JsScriptEngineService>()
                    .AddService<ITimerService, TimerService>()
                    .AddService<IEventLoopService, EventLoopService>()
                    .AddService<IDiagnosticService, DiagnosticService>();

                container.RegisterDelegate<IEventLoopTickDispatcher>(r => r.Resolve<IEventLoopService>());

                container.RegisterNetworkMessage<PingMessage>();


                container
                    .AddScriptModule<ConsoleModule>()
                    .AddScriptModule<LoggerModule>();

                container
                    .RegisterNetworkServices();

                return container;
            }
        );

        await bootstrap.RunAsync(cts.Token);
    }
);
