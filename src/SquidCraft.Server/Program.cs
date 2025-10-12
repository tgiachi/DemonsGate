using System.Text;
using ConsoleAppFramework;
using SquidCraft.Core.Extensions.Services;
using SquidCraft.Core.Interfaces.EventLoop;
using SquidCraft.Core.Json;
using SquidCraft.Core.Utils;
using SquidCraft.Entities.Extensions;
using SquidCraft.Lua.Scripting.Engine.Extensions.Scripts;
using SquidCraft.Network.Extensions;
using SquidCraft.Network.Messages.Auth;
using SquidCraft.Network.Messages.Pings;
using SquidCraft.Server;
using SquidCraft.Services.Context;
using SquidCraft.Services.Data.Config.Options;
using SquidCraft.Services.Impl;
using SquidCraft.Services.Interfaces;
using SquidCraft.Server.Modules;
using SquidCraft.Services.Modules;
using SquidCraft.Services.Types;
using DryIoc;
using MemoryPack;
using SquidCraft.Entities.Models;
using SquidCraft.Entities.Models.Base;
using SquidCraft.Game.Data.Context;
using SquidCraft.Lua.Scripting.Engine.Context;
using SquidCraft.Lua.Scripting.Engine.Services;
using SquidCraft.Services.Game.Data.Config;
using SquidCraft.Services.Game.Impl;
using SquidCraft.Services.Game.Interfaces;


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
        string pidFileName = "squidcraft.pid",
        LogLevelType logLevel = LogLevelType.Debug,
        string? rootDirectory = null,
        bool isShellEnabled = true,
        string configFileName = "squidcraft_server.json"
    ) =>
    {
        JsonUtils.RegisterJsonContext(SquidCraftJsonContext.Default);
        JsonUtils.RegisterJsonContext(SquidCraftLuaScriptJsonContext.Default);
        JsonUtils.RegisterJsonContext(SquidCraftGameJsonContext.Default);

        MemoryPackFormatterProvider.Register<BaseEntity>();
        MemoryPackFormatterProvider.Register<UserEntity>();
        MemoryPackFormatterProvider.RegisterCollection<List<UserEntity>, UserEntity>();


        var options = new SquidCraftServerOptions()
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

        var bootstrap = new SquidCraftBootstrap(options);

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
                    .AddService<IPlayerManagerService, PlayerManagerService>()
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
