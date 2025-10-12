using System.Numerics;
using Serilog;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Messages.Players;
using SquidCraft.Network.Types;
using SquidCraft.Services.Game.Data.Sessions;
using SquidCraft.Services.Game.Interfaces;

namespace SquidCraft.Services.Game.Impl;

public class PlayerManagerService : IPlayerManagerService
{
    private readonly ILogger _logger = Log.ForContext<PlayerManagerService>();

    private readonly INetworkManagerService _networkManagerService;

    private readonly IWorldManagerService _worldManagerService;

    public PlayerManagerService(INetworkManagerService networkManagerService, IWorldManagerService worldManagerService)
    {
        _networkManagerService = networkManagerService;
        _worldManagerService = worldManagerService;

        _networkManagerService.PlayerSessionAdded += OnPlayerSessionAdded;

        _networkManagerService.AddListener(NetworkMessageType.PlayerPositionRequest, OnPlayerPositionRequest);
    }

    private async Task OnPlayerPositionRequest(PlayerNetworkSession session, ISquidCraftMessage message)
    {
        var positionRequest = message as PlayerPositionRequest;

        session.Position = positionRequest.Position;
        session.Rotation = positionRequest.Rotation;


    }

    private void OnPlayerSessionAdded(PlayerNetworkSession session)
    {
        session.OnPositionChanged += SessionOnOnPositionChanged;
    }

    private async void SessionOnOnPositionChanged(Vector3 position)
    {
        var chunks = await _worldManagerService.GetChunksInRadius(position, Vector3.One);

        _logger.Information("Received player position update for chunk {Chunk}", chunks);

    }
}
