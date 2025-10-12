using DemonsGate.Network.Attributes;
using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Handshake;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.VersionResponse)]
public partial class VersionResponse : BaseDemonsGameMessage
{
    public string Version { get; set; }

    public VersionResponse() : base(NetworkMessageType.VersionResponse)
    {
    }

    public override string ToString()
    {
        return $"VersionResponse {Version}";
    }
}
