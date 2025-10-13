using System.Numerics;
using MemoryPack;
using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Messages.Players;

[NetworkMessage(NetworkMessageType.PlayerPositionRequest)]
[MemoryPackable]
public partial class PlayerPositionRequest : BaseSquidCraftGameMessage
{
    public Vector3 Position { get; set; }

    public Vector3 Rotation { get; set; }

    public PlayerPositionRequest() : base(NetworkMessageType.PlayerPositionRequest)
    {
    }
}
