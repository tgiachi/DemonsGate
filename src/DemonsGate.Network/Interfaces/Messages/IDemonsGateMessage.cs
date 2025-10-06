using DemonsGate.Network.Types;

namespace DemonsGate.Network.Interfaces.Messages;

public interface IDemonsGateMessage
{
    NetworkMessageType MessageType { get; }
}
