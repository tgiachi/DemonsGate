using System.Numerics;
using Serilog;
using SquidCraft.Game.Data.Utils;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Messages.Players;
using SquidCraft.Network.Types;
using SquidCraft.Services.Game.Data.Sessions;
using SquidCraft.Services.Game.Extensions;
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

    private async void SessionOnOnPositionChanged(PlayerNetworkSession session, Vector3 position)
    {
        _logger.Debug("Player position changed to {Position}, facing {SideView}", position, session.SideView);

        // Get chunk positions in the direction the player is facing
        const int chunksAhead = 3; // Number of chunks to load ahead
        var chunkPositions = ChunkUtils.GetChunksInDirection(position, session.SideView, chunksAhead);

        // Filter out chunks that have already been sent
        var unsentChunks = session.FilterUnsentChunks(chunkPositions).ToList();

        if (unsentChunks.Count == 0)
        {
            _logger.Debug("All chunks in direction {Direction} have already been sent", session.SideView);
            return;
        }

        _logger.Information(
            "Requesting {Count} new chunks (out of {Total}) ahead in direction {Direction} from position {Position}",
            unsentChunks.Count,
            chunkPositions.Count,
            session.SideView,
            position
        );

        // Request the chunks from the world manager
        var chunks = await _worldManagerService.GetChunksByPositions(unsentChunks);

        // Mark these chunks as sent
        session.MarkChunksAsSent(unsentChunks);

        if (chunks.ToList().Count > 0)
        {
            foreach (var chunk in chunks)
            {
                var chunkResponseMessage = new ChunkResponse();
                chunkResponseMessage.Chunks.Add(chunk);
                await session.SendMessages(chunkResponseMessage);
            }
        }


        _logger.Information(
            "Retrieved and sent {Count} chunks to player. Total sent chunks: {TotalSent}",
            chunks.Count(),
            session.SentChunkCount
        );
    }
}
