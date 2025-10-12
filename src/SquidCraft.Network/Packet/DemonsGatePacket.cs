using SquidCraft.Network.Types;
using MemoryPack;

namespace SquidCraft.Network.Packet;

/// <summary>
///     Represents a network packet in the DemonsGate protocol
/// </summary>
[MemoryPackable]
public partial class DemonsGatePacket
{
    /// <summary>
    ///     Gets or sets the message payload (serialized, potentially encrypted and/or compressed)
    /// </summary>
    public byte[] Payload { get; set; }

    /// <summary>
    ///     Gets or sets the type of message contained in this packet
    /// </summary>
    public NetworkMessageType MessageType { get; set; }

    /// <summary>
    ///     Gets or sets the flags applied to this packet (e.g., compressed, encrypted)
    /// </summary>
    public NetworkMessageFlagType FlagType { get; set; }

    /// <summary>
    ///     Returns a string representation of the packet
    /// </summary>
    /// <returns>A string containing the message type and payload length</returns>
    public override string ToString()
    {
        return $"[DemonsGatePacket: MessageType={MessageType}, PayloadLength={Payload?.Length ?? 0}]";
    }
}
