using DemonsGate.Network.Types;
using MemoryPack;

namespace DemonsGate.Network.Packet;

[MemoryPackable]
public partial class DemonsGatePacket
{
    public byte[] Payload { get; set; }

    public NetworkMessageType MessageType { get; set; }


    public override string ToString()
    {
        return $"[DemonsGatePacket: MessageType={MessageType}, PayloadLength={Payload?.Length ?? 0}]";
    }
}
