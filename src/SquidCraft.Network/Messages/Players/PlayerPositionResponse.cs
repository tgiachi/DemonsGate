using System.Numerics;
using MemoryPack;
using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Messages.Players;

[NetworkMessage(NetworkMessageType.PlayerPositionResponse)]
[MemoryPackable]
public partial class PlayerPositionResponse : BaseSquidCraftGameMessage
{
    public bool Success { get; set; }

    public Vector3 Position { get; set; }

    public Vector3 Rotation { get; set; }

    public PlayerPositionResponse() : base(NetworkMessageType.PlayerPositionResponse)
    {
    }
}
