using Serilog;
using SquidCraft.Client.Components.Base;
using SquidCraft.Network.Interfaces.Processors;
using SquidCraft.Network.Interfaces.Services;
using SquidCraft.Network.Processors;
using SquidCraft.Network.Services;
using SquidCraft.Services.Data.Config.Sections;
using SquidCraft.Services.Impl;

namespace SquidCraft.Client.Components.Networks;

public class NetworkClientComponent : BaseComponent
{

    private readonly ILogger _logger = Log.ForContext<NetworkClientComponent>();

    private readonly EventLoopService _eventLoopService;
    private readonly GameNetworkConfig _gameNetworkConfig;

    private readonly IPacketSerializer _packetSerializer;
    private readonly IPacketDeserializer _packetDeserializer;

    private readonly INetworkClientService _networkClientService;

    public NetworkClientComponent(GameNetworkConfig gameNetworkConfig)
    {
        _gameNetworkConfig = gameNetworkConfig;
        _eventLoopService = new EventLoopService(new EventBusService(), new EventLoopConfig());
        _packetSerializer =  new DefaultPacketProcessor(gameNetworkConfig);
        _packetDeserializer =  new DefaultPacketProcessor(gameNetworkConfig);
       _networkClientService = new DefaultNetworkClientService(
            _packetSerializer,
            _packetDeserializer,
            null, // Uses NetworkMessagesUtils.Messages by default
            _gameNetworkConfig,
            _eventLoopService
        );
    }

    public override async void Initialize()
    {
        await _eventLoopService.StartAsync();
        await _networkClientService.StartAsync();
        base.Initialize();
    }
}
