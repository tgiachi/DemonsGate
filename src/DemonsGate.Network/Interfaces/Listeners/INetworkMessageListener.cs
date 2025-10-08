using DemonsGate.Network.Interfaces.Messages;

namespace DemonsGate.Network.Interfaces.Listeners;

public interface INetworkMessageListener
{
    Task HandleMessageAsync(int sessionId, IDemonsGateMessage message);

}
