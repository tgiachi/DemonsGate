using SquidCraft.Core.Enums;
using SquidCraft.Core.Utils;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Interfaces.Processors;
using SquidCraft.Network.Packet;
using SquidCraft.Network.Types;
using SquidCraft.Services.Data.Config.Sections;
using MemoryPack;
using Serilog;

namespace SquidCraft.Network.Processors;

/// <summary>
/// Implements the packet processor for serializing and deserializing network messages.
/// </summary>
public class DefaultPacketProcessor : IPacketDeserializer, IPacketSerializer
{
    private readonly ILogger _logger = Log.ForContext<DefaultPacketProcessor>();

    private readonly GameNetworkConfig _networkConfig;

    private readonly Dictionary<byte, Func<byte[], ISquidCraftMessage>> _deserializers = new();

    public DefaultPacketProcessor(GameNetworkConfig networkConfig)
    {
        _networkConfig = networkConfig;
    }


    public async Task<ISquidCraftMessage> DeserializeAsync<T>(
        byte[] data, CancellationToken cancellationToken = default
    )
        where T : ISquidCraftMessage
    {
        _logger.Debug("Deserializing data of length {DataLength}", data.Length);

        var packet = MemoryPackSerializer.Deserialize<SquidCraftPacket>(data);
        if (packet == null)
        {
            throw new InvalidOperationException("Failed to deserialize SquidCraftPacket");
        }

        _logger.Debug(
            "Deserialized packet with MessageType {MessageType} and Payload length {PayloadLength}",
            packet.MessageType,
            packet.Payload.Length
        );

        byte[] payload = packet.Payload;

        // STEP 1: Decrypt first (if enabled)
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

        // STEP 2: Decompress second (if packet is compressed)
        if (packet.FlagType.HasFlag(NetworkMessageFlagType.Compressed))
        {
            var originalLength = payload.Length;

            payload = CompressionUtils.Decompress(
                new ReadOnlySpan<byte>(payload),
                _networkConfig.CompressionType
            );

            _logger.Debug(
                "Decompressed packet with {CompressionType}, compressed length {CompressedLength}, decompressed length {DecompressedLength}",
                _networkConfig.CompressionType,
                originalLength,
                payload.Length
            );
        }

        return !_deserializers.TryGetValue((byte)packet.MessageType, out var deserializer)
            ? throw new InvalidOperationException($"No deserializer registered for message type {packet.MessageType}")
            : deserializer(payload);
    }

    public void RegisterMessageType<T>() where T : ISquidCraftMessage, new()
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

    public void RegisterMessageType(Type type, NetworkMessageType messageType)
    {
        if (!typeof(ISquidCraftMessage).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type {type.Name} does not implement ISquidCraftMessage");
        }

        if (_deserializers.ContainsKey((byte)messageType))
        {
            _logger.Warning("Message type {MessageType} is already registered", messageType);
            return;
        }

        _deserializers[(byte)messageType] = (data) =>
        {
            var deserializedMessage = (ISquidCraftMessage?)MemoryPackSerializer.Deserialize(type, data);
            return deserializedMessage ??
                   throw new InvalidOperationException($"Failed to deserialize message of type {type.Name}");
        };

        _logger.Information("Registered message type {MessageType}", messageType);
    }


    public async Task<byte[]> SerializeAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : ISquidCraftMessage
    {
        _logger.Debug("Serializing message of type {MessageType}", message.MessageType);

        var packet = new SquidCraftPacket
        {
            MessageType = message.MessageType,
            Payload = MemoryPackSerializer.Serialize(message.GetType(), message)
        };

        // STEP 1: Compress first (if enabled)
        if (_networkConfig.CompressionType != CompressionType.None)
        {
            var originalPayload = packet.Payload;
            var originalLength = originalPayload.Length;

            var compressedPayload = CompressionUtils.Compress(
                new ReadOnlySpan<byte>(originalPayload),
                _networkConfig.CompressionType
            );

            // Only use compression if it actually reduces size
            if (compressedPayload.Length < originalLength)
            {
                packet.Payload = compressedPayload;
                packet.FlagType |= NetworkMessageFlagType.Compressed;

                _logger.Debug(
                    "Compressed packet with {CompressionType}, original length {OriginalLength}, new length {NewLength}, saved {SavedBytes} bytes",
                    _networkConfig.CompressionType,
                    originalLength,
                    packet.Payload.Length,
                    originalLength - packet.Payload.Length
                );
            }
            else
            {
                _logger.Debug(
                    "Compression with {CompressionType} did not reduce size (original: {OriginalLength}, compressed: {CompressedLength}), keeping uncompressed",
                    _networkConfig.CompressionType,
                    originalLength,
                    compressedPayload.Length
                );
            }
        }

        // STEP 2: Encrypt second (if enabled)
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
            packet.FlagType |= NetworkMessageFlagType.Encrypted;
        }

        return MemoryPackSerializer.Serialize(packet);
    }
}
