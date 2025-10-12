using DemonsGate.Network.Attributes;
using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Assets;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.AssetRequest)]
public partial class AssetRequestMessage : BaseDemonsGameMessage
{
    public string FileName { get; set; }

    public AssetRequestMessage() : base(NetworkMessageType.AssetRequest)
    {
    }


}
