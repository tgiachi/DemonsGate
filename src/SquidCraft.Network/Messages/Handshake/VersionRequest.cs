using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Handshake;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.VersionRequest)]
public partial class VersionRequest : BaseDemonsGameMessage
{
    public VersionRequest() : base(NetworkMessageType.VersionRequest)
    {
    }
}
