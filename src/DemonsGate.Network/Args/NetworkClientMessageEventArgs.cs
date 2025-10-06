using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Types;

namespace DemonsGate.Network.Args;

public record NetworkClientMessageEventArgs( int ClientId, IDemonsGateMessage Message, NetworkMessageType MessageType);

