using Serilog;
using SquidCraft.Network.Messages.Auth;
using SquidCraft.Services.Game.Data.Sessions;
using SquidCraft.Services.Game.Interfaces;

namespace SquidCraft.Services.Game.Handlers;

public class LoginHandler : IMessageHandler<LoginRequestMessage>
{
    private readonly ILogger _logger = Log.ForContext<LoginHandler>();

    public async Task HandleAsync(PlayerNetworkSession session, LoginRequestMessage message)
    {
        // Fake login success for now
        _logger.Information("Player {SessionId} logged in with username {Username}", session.SessionId, message.Email);
    }
}
