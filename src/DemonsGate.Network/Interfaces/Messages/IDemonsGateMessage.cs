using DemonsGate.Network.Types;

namespace DemonsGate.Network.Interfaces.Messages;

/// <summary>
/// Defines the contract for a Demons Gate network message.
/// </summary>
public interface IDemonsGateMessage
{
    NetworkMessageType MessageType { get; }
}
