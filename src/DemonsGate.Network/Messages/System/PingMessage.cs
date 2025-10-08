using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.System;

/// <summary>
///     Represents a ping message used to check connection status and measure latency
/// </summary>
[MemoryPackable]
public partial class PingMessage : BaseDemonsGameMessage
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
