using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Messages;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.SystemChat)]
public partial class SystemChatMessage : BaseDemonsGameMessage
{
    public string Message { get; set; }
    public SystemChatType Type { get; set; } = SystemChatType.Normal;

    public SystemChatMessage() : base(NetworkMessageType.SystemChat)
    {
    }
}
