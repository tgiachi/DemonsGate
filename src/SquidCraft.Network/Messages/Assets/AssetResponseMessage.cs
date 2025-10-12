using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Assets;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.AssetResponse)]
public partial class AssetResponseMessage : BaseSquidCraftGameMessage
{
    public AssetResponseMessage() : base(NetworkMessageType.AssetResponse)
    {
    }

    public string FileName { get; set; }
    public string Hash { get; set; }

    public byte[] Content { get; set; }
}
