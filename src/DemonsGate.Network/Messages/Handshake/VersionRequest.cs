using DemonsGate.Network.Attributes;
using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Handshake;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.VersionRequest)]
public partial class VersionRequest : BaseDemonsGameMessage
{
    public VersionRequest() : base(NetworkMessageType.VersionRequest)
    {
    }
}
