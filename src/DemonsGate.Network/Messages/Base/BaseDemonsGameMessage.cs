using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Types;

namespace DemonsGate.Network.Messages.Base;

public abstract class BaseDemonsGameMessage : IDemonsGateMessage
{
    public NetworkMessageType MessageType { get; }


    protected BaseDemonsGameMessage(NetworkMessageType type)
    {
        MessageType = type;
    }
}
