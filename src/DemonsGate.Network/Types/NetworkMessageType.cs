namespace DemonsGate.Network.Types;

/// <summary>
///     Defines the types of network messages supported by the DemonsGate protocol
/// </summary>
public enum NetworkMessageType : byte
{
    /// <summary>Ping message to check connection status</summary>
    Ping,

    /// <summary>Pong message in response to a ping</summary>
    Pong,
}
