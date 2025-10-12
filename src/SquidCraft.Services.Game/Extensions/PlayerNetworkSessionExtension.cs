using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Services.Game.Data.Sessions;

namespace SquidCraft.Services.Game.Extensions;

public static class PlayerNetworkSessionExtension
{
    public static async Task SendMessages<TMessage>(this PlayerNetworkSession session, TMessage message)
        where TMessage : ISquidCraftMessage
    {
        await session.NetworkManagerService.SendMessages(session, message);
    }

    public static async Task SendMessages(this PlayerNetworkSession session, params ISquidCraftMessage[] messages)
    {
        await session.NetworkManagerService.SendMessages(session, messages);
    }
}
