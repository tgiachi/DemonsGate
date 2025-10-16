using System.Numerics;
using Serilog;
using SquidCraft.Network.Messages.Auth;
using SquidCraft.Network.Messages.Players;
using SquidCraft.Services.Game.Data.Sessions;
using SquidCraft.Services.Game.Extensions;
using SquidCraft.Services.Game.Interfaces;

namespace SquidCraft.Services.Game.Handlers;

public class LoginHandler : IMessageHandler<LoginRequestMessage>
{
    private readonly ILogger _logger = Log.ForContext<LoginHandler>();

    public async Task HandleAsync(PlayerNetworkSession session, LoginRequestMessage message)
    {
        // Fake login success for now
        _logger.Information("Player {SessionId} logged in with username {Username}", session.SessionId, message.Email);

        session.IsLoggedIn = true;
        session.LastPing = DateTime.UtcNow;

        var response = new LoginResponseMessage
        {
            Success = true,
        };

        var playerResponse = new PlayerPositionResponse()
        {
            Position = Vector3.One,
            Rotation = Vector3.Zero
        };

        session.Position = playerResponse.Position;
        session.Rotation = playerResponse.Rotation;

        await session.SendMessages(response);

        await session.SendMessages(playerResponse);




    }
}
