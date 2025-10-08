using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Pings;

[MemoryPackable]
public partial class PongMessage : BaseDemonsGameMessage
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public PongMessage() : base(NetworkMessageType.Pong)
    {
    }
}
