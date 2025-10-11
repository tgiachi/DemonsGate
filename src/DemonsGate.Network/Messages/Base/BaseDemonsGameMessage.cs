using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Types;

namespace DemonsGate.Network.Messages.Base;

public abstract class BaseDemonsGameMessage : IDemonsGateMessage
{
    public NetworkMessageType MessageType { get; }

    /// <summary>
    /// Gets or sets the unique identifier for request/response tracking.
    /// </summary>
    public Guid? RequestId { get; set; }

    protected BaseDemonsGameMessage(NetworkMessageType type)
    {
        MessageType = type;
    }
}
