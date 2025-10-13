using System.Numerics;
using System.Reactive.Linq;
using SquidCraft.Network.Processors;
using SquidCraft.Network.Services;
using SquidCraft.Services.Data.Config.Sections;
using SquidCraft.Services.Impl;
using Serilog;
using SquidCraft.Network.Messages.Players;

Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(formatProvider: Thread.CurrentThread.CurrentCulture).CreateLogger();

var cancellationToken = new CancellationTokenSource();


Console.CancelKeyPress += (sender, eventArgs) => { cancellationToken.Cancel(); };


Log.Information("Starting SquidCraft console client");

var eventLoopService = new EventLoopService(new EventBusService(), new EventLoopConfig());
var config = new GameNetworkConfig();
var packetProcessor = new DefaultPacketProcessor(config);
var client = new DefaultNetworkClientService(
    packetProcessor,
    packetProcessor,
    null, // Uses NetworkMessagesUtils.Messages by default
    config,
    eventLoopService
);

await eventLoopService.StartAsync();
await client.StartAsync();

var position = Vector3.Zero;

client.Connected += (sender, eventArgs) =>
{
    Observable.Interval(TimeSpan.FromSeconds(1))
        .Subscribe(async l =>
            {
                await client.SendMessageAsync(
                    new PlayerPositionRequest()
                    {
                        Rotation = Vector3.Zero,
                        Position = position
                    }
                );

                position.X += .2f;
                position.Y += Random.Shared.NextSingle(); // random
            }
        );
    Log.Information("Connected");
};
client.MessageReceived += (sender, eventArgs) =>
{
    Log.Information("Message received {Type} {Message}", eventArgs.MessageType, eventArgs.Message);

};

await client.ConnectAsync("127.0.0.1", config.Port);

while (!cancellationToken.IsCancellationRequested)
{
    await Task.Delay(100);
}
