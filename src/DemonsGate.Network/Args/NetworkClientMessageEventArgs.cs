using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Types;

namespace DemonsGate.Network.Args;

/// <summary>
/// public record NetworkClientMessageEventArgs( int ClientId, IDemonsGateMessage Message, NetworkMessageType MessageType);.
/// </summary>
public record NetworkClientMessageEventArgs( int ClientId, IDemonsGateMessage Message, NetworkMessageType MessageType);

