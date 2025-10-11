using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Assets;

[MemoryPackable]
public partial class AssetResponseMessage : BaseDemonsGameMessage
{
    public AssetResponseMessage() : base(NetworkMessageType.AssetResponse)
    {
    }

    public string FileName { get; set; }
    public string Hash { get; set; }

    public byte[] Content { get; set; }
}
