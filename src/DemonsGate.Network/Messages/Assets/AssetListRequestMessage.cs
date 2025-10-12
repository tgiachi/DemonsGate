using DemonsGate.Network.Attributes;
using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Assets;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.AssetListRequest)]
public partial class AssetListRequestMessage : BaseDemonsGameMessage
{

    public AssetListRequestMessage() : base(NetworkMessageType.AssetListRequest)
    {

    }
}
