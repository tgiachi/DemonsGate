using MemoryPack;
using SquidCraft.Game.Data.Network;
using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Messages.Players;


[NetworkMessage(NetworkMessageType.ChunkResponse)]
[MemoryPackable]
public partial class ChunkResponse : BaseSquidCraftGameMessage
{
    public List<SerializableChunkEntity> Chunks { get; set; } = new();
    public ChunkResponse() : base(NetworkMessageType.ChunkResponse)
    {
    }
}
