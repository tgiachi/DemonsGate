using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Pings;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.Pong)]
public partial class PongMessage : BaseDemonsGameMessage
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public PongMessage() : base(NetworkMessageType.Pong)
    {
    }
}
