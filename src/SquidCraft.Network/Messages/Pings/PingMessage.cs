using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Pings;

/// <summary>
///     Represents a ping message used to check connection status and measure latency
/// </summary>
[MemoryPackable]
[NetworkMessage(NetworkMessageType.Ping)]
public partial class PingMessage : BaseSquidCraftGameMessage
{
    /// <summary>
    ///     Gets or sets the timestamp when the ping was sent
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PingMessage"/> class
    /// </summary>
    public PingMessage() : base(NetworkMessageType.Ping)
    {
    }
}
