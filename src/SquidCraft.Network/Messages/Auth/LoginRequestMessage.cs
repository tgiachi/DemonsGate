using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Auth;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.LoginRequest)]
public partial class LoginRequestMessage  : BaseDemonsGameMessage
{
    public string Email { get; set; }

    public string Password { get; set; }

    public LoginRequestMessage() : base(NetworkMessageType.LoginRequest)
    {
    }


}
