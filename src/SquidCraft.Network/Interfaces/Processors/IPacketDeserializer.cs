using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Interfaces.Processors;

/// <summary>
///     Defines a contract for deserializing network packets into messages
/// </summary>
public interface IPacketDeserializer
{
    /// <summary>
    ///     Deserializes packet data into a message of the specified type
    /// </summary>
    /// <typeparam name="T">The expected message type</typeparam>
    /// <param name="data">The serialized packet data</param>
    /// <param name="cancellationToken">Cancellation token for the async operation</param>
    /// <returns>A task that resolves to the deserialized message</returns>
    Task<ISquidCraftMessage> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        where T : ISquidCraftMessage;

    /// <summary>
    ///     Registers a message type for deserialization
    /// </summary>
    /// <typeparam name="T">The message type to register</typeparam>
    void RegisterMessageType<T>() where T : ISquidCraftMessage, new();

    /// <summary>
    ///     Registers a message type for deserialization using reflection
    /// </summary>
    /// <param name="type">The message type to register</param>
    /// <param name="messageType">The network message type identifier</param>
    void RegisterMessageType(Type type, NetworkMessageType messageType);
}
