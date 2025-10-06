using DemonsGate.Network.Types;

namespace DemonsGate.Network.Data.Services;

/// <summary>
///     Represents metadata for a registered network message type
/// </summary>
/// <param name="type">The CLR type of the message</param>
/// <param name="MessageType">The network message type identifier</param>
public record NetworkMessageData(Type type, NetworkMessageType MessageType);

