using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Messages.Base;

/// <summary>
/// Base class for all Demons Gate network messages providing common functionality.
/// </summary>
public abstract class BaseDemonsGameMessage : ISquidCraftMessage
{
    /// <summary>
    /// Gets the type of this network message.
    /// </summary>
    public NetworkMessageType MessageType { get; }

    /// <summary>
    /// Gets or sets the unique identifier for request/response tracking.
    /// When set, this allows the system to match responses to their corresponding requests,
    /// enabling multiple concurrent requests of the same message type.
    /// </summary>
    public Guid? RequestId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDemonsGameMessage"/> class.
    /// </summary>
    /// <param name="type">The type of network message</param>
    protected BaseDemonsGameMessage(NetworkMessageType type)
    {
        MessageType = type;
    }
}
