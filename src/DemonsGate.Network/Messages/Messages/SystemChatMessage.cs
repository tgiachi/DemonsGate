using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Messages;

[MemoryPackable]
public partial class SystemChatMessage : BaseDemonsGameMessage
{
    public string Message { get; set; }
    public SystemChatType Type { get; set; } = SystemChatType.Normal;

    public SystemChatMessage() : base(NetworkMessageType.SystemChat)
    {
    }
}
