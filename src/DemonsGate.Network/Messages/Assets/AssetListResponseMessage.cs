using DemonsGate.Network.Attributes;
using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Assets;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.AssetListResponse)]
public partial class AssetListResponseMessage : BaseDemonsGameMessage
{
    public List<AssetEntry> NameList { get; set; }

    public AssetListResponseMessage() : base(NetworkMessageType.AssetListResponse)
    {
    }
}
