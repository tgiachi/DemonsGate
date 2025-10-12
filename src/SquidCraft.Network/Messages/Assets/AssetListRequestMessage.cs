using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Assets;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.AssetListRequest)]
public partial class AssetListRequestMessage : BaseDemonsGameMessage
{

    public AssetListRequestMessage() : base(NetworkMessageType.AssetListRequest)
    {

    }
}
