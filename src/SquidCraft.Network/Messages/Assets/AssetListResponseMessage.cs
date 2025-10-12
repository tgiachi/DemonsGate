using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Assets;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.AssetListResponse)]
public partial class AssetListResponseMessage : BaseDemonsGameMessage
{
    public List<AssetEntry> NameList { get; set; }

    public AssetListResponseMessage() : base(NetworkMessageType.AssetListResponse)
    {
    }
}
