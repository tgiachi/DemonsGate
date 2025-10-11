using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Assets;

[MemoryPackable]
public partial class AssetRequestMessage : BaseDemonsGameMessage
{
    public string FileName { get; set; }

    public AssetRequestMessage() : base(NetworkMessageType.AssetRequest)
    {
    }


}
