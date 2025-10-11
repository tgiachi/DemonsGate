using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Handshake;

[MemoryPackable]
public partial class VersionResponse  : BaseDemonsGameMessage
{
    public VersionResponse() : base(NetworkMessageType.VersionResponse)
    {

    }
}
