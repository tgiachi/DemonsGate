using DemonsGate.Network.Types;

namespace DemonsGate.Network.Interfaces.Messages;

/// <summary>
/// Defines the contract for a Demons Gate network message.
/// </summary>
public interface IDemonsGateMessage
{
    /// <summary>
    /// Gets the type of network message.
    /// </summary>
    NetworkMessageType MessageType { get; }

    /// <summary>
    /// Gets or sets the unique identifier for request/response tracking.
    /// Null for messages that don't require tracking (e.g., one-way messages).
    /// </summary>
    Guid? RequestId { get; set; }
}
