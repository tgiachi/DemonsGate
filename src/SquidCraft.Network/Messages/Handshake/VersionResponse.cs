using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Handshake;

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
