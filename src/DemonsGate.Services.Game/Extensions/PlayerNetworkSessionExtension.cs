using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Services.Game.Data.Sessions;

namespace DemonsGate.Services.Game.Extensions;

public static class PlayerNetworkSessionExtension
{
    public static async Task SendMessages<TMessage>(this PlayerNetworkSession session, TMessage message)
        where TMessage : IDemonsGateMessage
    {
        await session.NetworkManagerService.SendMessages(session, message);
    }

    public static async Task SendMessages(this PlayerNetworkSession session, params IDemonsGateMessage[] messages)
    {
        await session.NetworkManagerService.SendMessages(session, messages);
    }
}
