using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Assets;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.AssetRequest)]
public partial class AssetRequestMessage : BaseSquidCraftGameMessage
{
    public string FileName { get; set; }

    public AssetRequestMessage() : base(NetworkMessageType.AssetRequest)
    {
    }


}
