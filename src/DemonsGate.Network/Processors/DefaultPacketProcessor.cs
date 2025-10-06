using System.Text;
using DemonsGate.Core.Enums;
using DemonsGate.Core.Utils;
using DemonsGate.Network.Data;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Interfaces.Processors;
using DemonsGate.Network.Packet;
using MemoryPack;
using Serilog;

namespace DemonsGate.Network.Processors;

public class DefaultPacketProcessor : IPacketDeserializer, IPacketSerializer
{
    private readonly ILogger _logger = Log.ForContext<DefaultPacketProcessor>();

    private readonly NetworkConfig _networkConfig;

    private readonly Dictionary<byte, Func<byte[], IDemonsGateMessage>> _deserializers = new();

    public DefaultPacketProcessor(NetworkConfig networkConfig)
    {
        _networkConfig = networkConfig;
    }


    public async Task<IDemonsGateMessage> DeserializeAsync<
        T>(
        byte[] data, CancellationToken cancellationToken = default
    )
        where T : IDemonsGateMessage
    {
        _logger.Debug("Deserializing data of length {DataLength}", data.Length);

        var packet = MemoryPackSerializer.Deserialize<DemonsGatePacket>(data);
        if (packet == null)
        {
            throw new InvalidOperationException("Failed to deserialize DemonsGatePacket");
        }

        _logger.Debug(
            "Deserialized packet with MessageType {MessageType} and Payload length {PayloadLength}",
            packet.MessageType,
            packet.Payload.Length
        );

        byte[] payload = packet.Payload;

        if (_networkConfig.CompressionType != CompressionType.None)
        {
            var originalLength = payload.Length;

            payload = CompressionUtils.Decompress(
                new ReadOnlySpan<byte>(payload),
                _networkConfig.CompressionType
            );

            _logger.Debug(
                "Decompressed packet with {CompressionType}, original length {OriginalLength}, new length {NewLength}",
                _networkConfig.CompressionType,
                originalLength,
                payload.Length
            );
        }

        if (_networkConfig.EncryptionType != EncryptionType.None)
        {
            var originalLength = payload.Length;

            payload = EncryptionUtils.Decrypt(
                new ReadOnlySpan<byte>(payload),
                new ReadOnlySpan<byte>(Convert.FromBase64String(_networkConfig.EncryptionKey)),
                _networkConfig.EncryptionType
            );

            _logger.Debug(
                "Decrypted packet with {EncryptionType}, original length {OriginalLength}, new length {NewLength}",
                _networkConfig.EncryptionType,
                originalLength,
                payload.Length
            );
        }

        return !_deserializers.TryGetValue((byte)packet.MessageType, out var deserializer)
            ? throw new InvalidOperationException($"No deserializer registered for message type {packet.MessageType}")
            : deserializer(payload);
    }

    public void RegisterMessageType<T>() where T : IDemonsGateMessage, new()
    {
        var message = new T();
        var messageType = message.MessageType;

        if (_deserializers.ContainsKey((byte)messageType))
        {
            _logger.Warning("Message type {MessageType} is already registered", messageType);
            return;
        }

        _deserializers[(byte)messageType] = (data) =>
        {
            var deserializedMessage = MemoryPackSerializer.Deserialize<T>(data);
            return deserializedMessage ??
                   throw new InvalidOperationException($"Failed to deserialize message of type {typeof(T).Name}");
        };

        _logger.Information("Registered message type {MessageType}", messageType);
    }


    public async Task<DemonsGatePacket> SerializeAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : IDemonsGateMessage
    {
        _logger.Debug("Serializing message of type {MessageType}", message.MessageType);

        var packet = new DemonsGatePacket
        {
            MessageType = message.MessageType,
            Payload = MemoryPackSerializer.Serialize(message.GetType(), message)
        };

        if (_networkConfig.EncryptionType != EncryptionType.None)
        {
            var originalLength = packet.Payload.Length;

            packet.Payload = EncryptionUtils.Encrypt(
                new ReadOnlySpan<byte>(packet.Payload),
                new ReadOnlySpan<byte>(Convert.FromBase64String(_networkConfig.EncryptionKey)),
                _networkConfig.EncryptionType
            );

            _logger.Debug(
                "Encrypted packet with {EncryptionType}, original length {OriginalLength}, new length {NewLength}",
                _networkConfig.EncryptionType,
                originalLength,
                packet.Payload.Length
            );
        }

        if (_networkConfig.CompressionType != CompressionType.None)
        {
            var originalLength = packet.Payload.Length;

            packet.Payload = CompressionUtils.Compress(
                new ReadOnlySpan<byte>(packet.Payload),
                _networkConfig.CompressionType
            );

            _logger.Debug(
                "Compressed packet with {CompressionType}, original length {OriginalLength}, new length {NewLength}",
                _networkConfig.CompressionType,
                originalLength,
                packet.Payload.Length
            );
        }

        return packet;
    }
}
