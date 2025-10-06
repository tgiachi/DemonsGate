using DemonsGate.Network.Interfaces.Messages;

namespace DemonsGate.Network.Interfaces.Processors;

public interface IPacketDeserializer
{
    Task<IDemonsGateMessage> DeserializeAsync<T>(byte[] data, CancellationToken cancellationToken = default)
        where T : IDemonsGateMessage;

    void RegisterMessageType<T>() where T : IDemonsGateMessage, new();
}
