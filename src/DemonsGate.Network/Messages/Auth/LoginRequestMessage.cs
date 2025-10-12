using DemonsGate.Network.Attributes;
using DemonsGate.Network.Messages.Base;
using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Messages.Auth;

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
