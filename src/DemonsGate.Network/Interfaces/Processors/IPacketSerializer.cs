using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Packet;

namespace DemonsGate.Network.Interfaces.Processors;

public interface IPacketSerializer
{
    Task<DemonsGatePacket> SerializeAsync<T>(T message, CancellationToken cancellationToken = default) where T : IDemonsGateMessage;

}
