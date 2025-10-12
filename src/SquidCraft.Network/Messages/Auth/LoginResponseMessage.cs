using SquidCraft.Network.Attributes;
using SquidCraft.Network.Messages.Base;
using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Messages.Auth;

[MemoryPackable]
[NetworkMessage(NetworkMessageType.LoginResponse)]
public partial class LoginResponseMessage : BaseDemonsGameMessage
{
    public bool Success { get; set; }


    public LoginResponseMessage() : base(NetworkMessageType.LoginResponse)
    {
        Success = false;
    }

}
