using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Packet;

namespace DemonsGate.Network.Interfaces.Processors;

/// <summary>
///     Defines a contract for serializing messages into network packets
/// </summary>
public interface IPacketSerializer
{
    /// <summary>
    ///     Serializes a message into a network packet
    /// </summary>
    /// <typeparam name="T">The type of message to serialize</typeparam>
    /// <param name="message">The message to serialize</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A task that resolves to the serialized packet</returns>
    Task<DemonsGatePacket> SerializeAsync<T>(T message, CancellationToken cancellationToken = default) where T : IDemonsGateMessage;
}
