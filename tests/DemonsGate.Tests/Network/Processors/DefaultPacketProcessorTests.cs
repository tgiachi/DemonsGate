using System.Text;
using DemonsGate.Core.Enums;
using DemonsGate.Core.Utils;
using DemonsGate.Network.Data;
using DemonsGate.Network.Messages;
using DemonsGate.Network.Packet;
using DemonsGate.Network.Processors;
using MemoryPack;

namespace DemonsGate.Tests.Network.Processors;

[TestFixture]
public class DefaultPacketProcessorTests
{
    private NetworkConfig _networkConfig = null!;
    private DefaultPacketProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _networkConfig = new NetworkConfig
        {
            CompressionType = CompressionType.None,
            EncryptionType = EncryptionType.None,
            EncryptionKey = string.Empty
        };
        _processor = new DefaultPacketProcessor(_networkConfig);
        _processor.RegisterMessageType<PingMessage>();
    }

    [Test]
    public async Task SerializeAsync_ShouldSerializePingMessage()
    {
        // Arrange
        var message = new PingMessage();

        // Act
        var packet = await _processor.SerializeAsync(message);

        // Assert
        Assert.That(packet, Is.Not.Null);
        Assert.That(packet.MessageType, Is.EqualTo(message.MessageType));
        Assert.That(packet.Payload, Is.Not.Empty);
    }

    [Test]
    public async Task DeserializeAsync_ShouldDeserializePingMessage()
    {
        // Arrange
        var originalMessage = new PingMessage();
        var packet = await _processor.SerializeAsync(originalMessage);
        var packetBytes = MemoryPackSerializer.Serialize(packet);

        // Act
        var deserializedMessage = await _processor.DeserializeAsync<PingMessage>(packetBytes);

        // Assert
        Assert.That(deserializedMessage, Is.Not.Null);
        Assert.That(deserializedMessage.MessageType, Is.EqualTo(originalMessage.MessageType));
    }

    [Test]
    public async Task SerializeDeserialize_WithNoCompressionNoEncryption_ShouldRoundTrip()
    {
        // Arrange
        var message = new PingMessage();

        // Act
        var packet = await _processor.SerializeAsync(message);
        var packetBytes = MemoryPackSerializer.Serialize(packet);
        var deserializedMessage = await _processor.DeserializeAsync<PingMessage>(packetBytes);

        // Assert
        Assert.That(deserializedMessage.MessageType, Is.EqualTo(message.MessageType));
    }

    [Test]
    [TestCase(CompressionType.Brotli)]
    [TestCase(CompressionType.GZip)]
    [TestCase(CompressionType.Deflate)]
    [TestCase(CompressionType.LZ4)]
    public async Task SerializeDeserialize_WithCompression_ShouldRoundTrip(CompressionType compressionType)
    {
        // Arrange
        _networkConfig.CompressionType = compressionType;
        var message = new PingMessage();

        // Act
        var packet = await _processor.SerializeAsync(message);
        var packetBytes = MemoryPackSerializer.Serialize(packet);
        var deserializedMessage = await _processor.DeserializeAsync<PingMessage>(packetBytes);

        // Assert
        Assert.That(deserializedMessage.MessageType, Is.EqualTo(message.MessageType));
    }

    [Test]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public async Task SerializeDeserialize_WithEncryption_ShouldRoundTrip(EncryptionType encryptionType)
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(encryptionType);
        _networkConfig.EncryptionType = encryptionType;
        _networkConfig.EncryptionKey = Convert.ToBase64String(key);

        var message = new PingMessage();

        // Act
        var packet = await _processor.SerializeAsync(message);
        var packetBytes = MemoryPackSerializer.Serialize(packet);
        var deserializedMessage = await _processor.DeserializeAsync<PingMessage>(packetBytes);

        // Assert
        Assert.That(deserializedMessage.MessageType, Is.EqualTo(message.MessageType));
    }

    [Test]
    public async Task SerializeDeserialize_WithCompressionAndEncryption_ShouldRoundTrip()
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(EncryptionType.AES256);
        _networkConfig.CompressionType = CompressionType.LZ4;
        _networkConfig.EncryptionType = EncryptionType.AES256;
        _networkConfig.EncryptionKey = Convert.ToBase64String(key);

        var message = new PingMessage();

        // Act
        var packet = await _processor.SerializeAsync(message);
        var packetBytes = MemoryPackSerializer.Serialize(packet);
        var deserializedMessage = await _processor.DeserializeAsync<PingMessage>(packetBytes);

        // Assert
        Assert.That(deserializedMessage.MessageType, Is.EqualTo(message.MessageType));
    }

    [Test]
    public async Task Serialize_WithCompression_ShouldCompressPayload()
    {
        // Arrange
        _networkConfig.CompressionType = CompressionType.GZip;
        var message = new PingMessage();

        // Act - Serialize without compression first
        var uncompressedPacket = await new DefaultPacketProcessor(new NetworkConfig()).SerializeAsync(message);

        // Act - Serialize with compression
        var compressedPacket = await _processor.SerializeAsync(message);

        // Assert - The payload should be different (compressed)
        Assert.That(compressedPacket.Payload, Is.Not.EqualTo(uncompressedPacket.Payload));
    }

    [Test]
    public async Task Serialize_WithEncryption_ShouldEncryptPayload()
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(EncryptionType.AES256);
        _networkConfig.EncryptionType = EncryptionType.AES256;
        _networkConfig.EncryptionKey = Convert.ToBase64String(key);

        var message = new PingMessage();

        // Act - Serialize without encryption first
        var unencryptedPacket = await new DefaultPacketProcessor(new NetworkConfig()).SerializeAsync(message);

        // Act - Serialize with encryption
        var encryptedPacket = await _processor.SerializeAsync(message);

        // Assert - The payload should be different (encrypted)
        Assert.That(encryptedPacket.Payload, Is.Not.EqualTo(unencryptedPacket.Payload));
    }

    [Test]
    public void RegisterMessageType_ShouldRegisterMessageType()
    {
        // Arrange
        var newProcessor = new DefaultPacketProcessor(_networkConfig);

        // Act
        Assert.DoesNotThrow(() => newProcessor.RegisterMessageType<PingMessage>());
    }

    [Test]
    public async Task DeserializeAsync_WithoutRegisteredMessageType_ShouldThrowException()
    {
        // Arrange
        var processorWithoutRegistration = new DefaultPacketProcessor(_networkConfig);
        var message = new PingMessage();
        var packet = await _processor.SerializeAsync(message);
        var packetBytes = MemoryPackSerializer.Serialize(packet);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await processorWithoutRegistration.DeserializeAsync<PingMessage>(packetBytes);
        });
        Assert.That(ex!.Message, Does.Contain("No deserializer registered"));
    }

    [Test]
    public async Task Serialize_WithWrongEncryptionKey_ShouldFailDecryption()
    {
        // Arrange
        var correctKey = EncryptionUtils.GenerateKey(EncryptionType.AES256);
        var wrongKey = EncryptionUtils.GenerateKey(EncryptionType.AES256);

        var serializeConfig = new NetworkConfig
        {
            EncryptionType = EncryptionType.AES256,
            EncryptionKey = Convert.ToBase64String(correctKey)
        };

        var deserializeConfig = new NetworkConfig
        {
            EncryptionType = EncryptionType.AES256,
            EncryptionKey = Convert.ToBase64String(wrongKey)
        };

        var serializeProcessor = new DefaultPacketProcessor(serializeConfig);
        var deserializeProcessor = new DefaultPacketProcessor(deserializeConfig);
        deserializeProcessor.RegisterMessageType<PingMessage>();

        var message = new PingMessage();
        var packet = await serializeProcessor.SerializeAsync(message);
        var packetBytes = MemoryPackSerializer.Serialize(packet);

        // Act & Assert
        Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(async () =>
        {
            await deserializeProcessor.DeserializeAsync<PingMessage>(packetBytes);
        });
    }
}
