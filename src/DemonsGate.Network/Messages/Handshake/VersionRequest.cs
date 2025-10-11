using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Handshake;

[MemoryPackable]
public partial class VersionRequest : BaseDemonsGameMessage
{
    public VersionRequest() : base(NetworkMessageType.VersionRequest)
    {
    }
}
