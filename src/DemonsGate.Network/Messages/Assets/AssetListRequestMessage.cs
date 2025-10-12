using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Assets;

[MemoryPackable]
public partial class AssetListRequestMessage : BaseDemonsGameMessage
{

    public AssetListRequestMessage() : base(NetworkMessageType.AssetListRequest)
    {

    }
}
