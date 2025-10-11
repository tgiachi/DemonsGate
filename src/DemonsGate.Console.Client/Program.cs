using DemonsGate.Network.Data.Services;
using DemonsGate.Network.Messages.Assets;
using DemonsGate.Network.Messages.Auth;
using DemonsGate.Network.Messages.Handshake;
using DemonsGate.Network.Messages.Messages;
using DemonsGate.Network.Messages.Pings;
using DemonsGate.Network.Processors;
using DemonsGate.Network.Services;
using DemonsGate.Network.Types;
using DemonsGate.Services.Data.Config.Sections;
using DemonsGate.Services.Impl;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(formatProvider: Thread.CurrentThread.CurrentCulture).CreateLogger();

var cancellationToken = new CancellationTokenSource();


Console.CancelKeyPress += (sender, eventArgs) => { cancellationToken.Cancel(); };

var listOfRegisterMessages = new List<NetworkMessageData>
{
    new(typeof(PingMessage), NetworkMessageType.Ping),
    new(typeof(PongMessage), NetworkMessageType.Pong),
    new(typeof(LoginRequestMessage), NetworkMessageType.LoginRequest),
    new(typeof(LoginResponseMessage), NetworkMessageType.LoginResponse),
    new(typeof(SystemChatMessage), NetworkMessageType.SystemChat),
    new(typeof(AssetRequestMessage), NetworkMessageType.AssetRequest),
    new(typeof(AssetResponseMessage), NetworkMessageType.AssetResponse),
    new(typeof(VersionRequest), NetworkMessageType.VersionRequest),
    new(typeof(VersionResponse), NetworkMessageType.VersionResponse)
};


Log.Information("Starting DemonsGate console client");

var eventLoopService = new EventLoopService(new EventBusService(), new EventLoopConfig());
var config = new GameNetworkConfig();
var packetProcessor = new DefaultPacketProcessor(config);
var client = new DefaultNetworkClientService(
    packetProcessor,
    packetProcessor,
    listOfRegisterMessages,
    config,
    eventLoopService
);

await eventLoopService.StartAsync();
await client.StartAsync();

client.Connected += (sender, eventArgs) => { Log.Information("Connected"); };
client.MessageReceived += (sender, eventArgs) =>
{
    Log.Information("Message received {Type} {Message}", eventArgs.MessageType, eventArgs.Message);
};

await client.ConnectAsync("127.0.0.1", config.Port);

while (!cancellationToken.IsCancellationRequested)
{
    await Task.Delay(100);
}
