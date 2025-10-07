using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Types;

namespace DemonsGate.Network.Args;

/// <summary>
/// Represents event arguments for message reception.
/// </summary>
public record NetworkClientMessageEventArgs( int ClientId, IDemonsGateMessage Message, NetworkMessageType MessageType);

