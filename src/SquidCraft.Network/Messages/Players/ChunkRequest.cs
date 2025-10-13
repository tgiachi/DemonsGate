using System.Numerics;
using MemoryPack;
using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Messages.Players;


[NetworkMessage(NetworkMessageType.ChunkRequest)]
[MemoryPackable]
public partial class ChunkRequest : BaseSquidCraftGameMessage
{
    public List<Vector3>  Positions { get; set; }

    public ChunkRequest() : base(NetworkMessageType.ChunkRequest)
    {
    }
}
