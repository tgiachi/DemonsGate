using DemonsGate.Network.Types;

namespace DemonsGate.Network.Interfaces.Messages;

/// <summary>
/// public interface IDemonsGateMessage.
/// </summary>
public interface IDemonsGateMessage
{
    NetworkMessageType MessageType { get; }
}
