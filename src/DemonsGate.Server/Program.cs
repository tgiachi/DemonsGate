using System.Text;
using ConsoleAppFramework;
using DemonsGate.Core.Extensions.Services;
using DemonsGate.Core.Interfaces.EventLoop;
using DemonsGate.Core.Json;
using DemonsGate.Core.Utils;
using DemonsGate.Entities.Extensions;
using DemonsGate.Lua.Scripting.Engine.Extensions.Scripts;
using DemonsGate.Network.Extensions;
using DemonsGate.Network.Messages.Auth;
using DemonsGate.Network.Messages.Pings;
using DemonsGate.Server;
using DemonsGate.Services.Context;
using DemonsGate.Services.Data.Config.Options;
using DemonsGate.Services.Impl;
using DemonsGate.Services.Interfaces;
using DemonsGate.Server.Modules;
using DemonsGate.Services.Modules;
using DemonsGate.Services.Types;
using DryIoc;
using MemoryPack;
using DemonsGate.Entities.Models;
using DemonsGate.Entities.Models.Base;
using DemonsGate.Lua.Scripting.Engine.Context;
using DemonsGate.Lua.Scripting.Engine.Services;
using DemonsGate.Services.Game.Data.Config;
using DemonsGate.Services.Game.Impl;
using DemonsGate.Services.Game.Interfaces;


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
        LogLevelType logLevel = LogLevelType.Debug,
        string? rootDirectory = null,
        bool isShellEnabled = true,
        string configFileName = "demonsgate_server.json"
    ) =>
    {
        JsonUtils.RegisterJsonContext(DemonsGateJsonContext.Default);
        JsonUtils.RegisterJsonContext(DemonsGateLuaScriptJsonContext.Default);


        MemoryPackFormatterProvider.Register<BaseEntity>();
        MemoryPackFormatterProvider.Register<UserEntity>();
        MemoryPackFormatterProvider.RegisterCollection<List<UserEntity>, UserEntity>();


        var options = new DemonsGateServerOptions()
        {
            LogLevel = logLevel,
            PidFileName = pidFileName,
            RootDirectory = rootDirectory,
            ConfigFileName = configFileName,
            IsShellEnabled = isShellEnabled
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
                    .AddService<IScriptEngineService, LuaScriptEngineService>()
                    .AddService<ITimerService, TimerService>()
                    .AddService<IEventLoopService, EventLoopService>()
                    .AddService<IDiagnosticService, DiagnosticService>()
                    .AddService<ICommandService, CommandService>()
                    .AddService<ISeedService, SeedService>(100)
                    ;


                // Registering game services

                container
                    .AddService<IChunkGeneratorService, ChunkGeneratorService>(101)
                    .AddService<IWorldManagerService, WorldManagerService>()
                    .AddService<INetworkManagerService, NetworkManagerService>()
                    ;

                container.RegisterInstance(new ChunkGeneratorConfig());

                container.RegisterDelegate<IEventLoopTickDispatcher>(r => r.Resolve<IEventLoopService>());

                container
                    .RegisterNetworkMessage<PingMessage>()
                    .RegisterNetworkMessage<PongMessage>()
                    .RegisterNetworkMessage<LoginRequestMessage>()
                    .RegisterNetworkMessage<LoginResponseMessage>()
                    ;

                container.RegisterEntityServices();


                container
                    .AddLuaScriptModule<CommandModule>()
                    .AddLuaScriptModule<ConsoleModule>()
                    .AddLuaScriptModule<LoggerModule>()
                    ;

                container
                    .RegisterNetworkServices();

                return container;
            }
        );


        await bootstrap.RunAsync(cts.Token);
    }
);
