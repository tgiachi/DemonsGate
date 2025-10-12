using DemonsGate.Network.Attributes;
using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Auth;

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
