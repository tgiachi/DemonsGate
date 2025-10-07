namespace DemonsGate.Network.Args;

/// <summary>
/// Represents event arguments for raw message reception.
/// </summary>
public record NetworkClientRawMessageArgs(int ClientId, byte[] Data);
