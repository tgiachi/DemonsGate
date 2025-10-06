using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages;

[MemoryPackable]
public partial class PingMessage : BaseDemonsGameMessage
{
    public PingMessage() : base(NetworkMessageType.Ping)
    {
    }
}
