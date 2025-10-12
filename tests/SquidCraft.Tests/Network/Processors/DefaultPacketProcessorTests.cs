using SquidCraft.Core.Enums;
using SquidCraft.Core.Utils;
using SquidCraft.Network.Messages;
using SquidCraft.Network.Messages.Pings;
using SquidCraft.Network.Packet;
using SquidCraft.Network.Processors;
using SquidCraft.Network.Types;
using SquidCraft.Services.Data.Config.Sections;
using MemoryPack;

namespace SquidCraft.Tests.Network.Processors;

[TestFixture]
/// <summary>
/// Contains test cases for DefaultPacketProcessor.
/// </summary>
public class DefaultPacketProcessorTests
{
    private GameNetworkConfig _networkConfig = null!;
    private DefaultPacketProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _networkConfig = new GameNetworkConfig
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
        var packetBytes = await _processor.SerializeAsync(message);

        // Assert
        Assert.That(packetBytes, Is.Not.Null);
        Assert.That(packetBytes, Is.Not.Empty);
    }

    [Test]
    public async Task DeserializeAsync_ShouldDeserializePingMessage()
    {
        // Arrange
        var originalMessage = new PingMessage();
        var packetBytes = await _processor.SerializeAsync(originalMessage);

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
        var packetBytes = await _processor.SerializeAsync(message);
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
        var packetBytes = await _processor.SerializeAsync(message);
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
        var packetBytes = await _processor.SerializeAsync(message);
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
        var packetBytes = await _processor.SerializeAsync(message);
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
        var uncompressedBytes = await new DefaultPacketProcessor(new GameNetworkConfig()).SerializeAsync(message);

        // Act - Serialize with compression
        var compressedBytes = await _processor.SerializeAsync(message);

        // Assert - The bytes should be different (compressed)
        Assert.That(compressedBytes, Is.Not.EqualTo(uncompressedBytes));
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
        var unencryptedBytes = await new DefaultPacketProcessor(new GameNetworkConfig()).SerializeAsync(message);

        // Act - Serialize with encryption
        var encryptedBytes = await _processor.SerializeAsync(message);

        // Assert - The bytes should be different (encrypted)
        Assert.That(encryptedBytes, Is.Not.EqualTo(unencryptedBytes));
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
        var processorWithRegistration = new DefaultPacketProcessor(_networkConfig);
        processorWithRegistration.RegisterMessageType<PingMessage>();

        var processorWithoutRegistration = new DefaultPacketProcessor(_networkConfig);
        var message = new PingMessage();
        var packetBytes = await processorWithRegistration.SerializeAsync(message);

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

        var serializeConfig = new GameNetworkConfig
        {
            EncryptionType = EncryptionType.AES256,
            EncryptionKey = Convert.ToBase64String(correctKey)
        };

        var deserializeConfig = new GameNetworkConfig
        {
            EncryptionType = EncryptionType.AES256,
            EncryptionKey = Convert.ToBase64String(wrongKey)
        };

        var serializeProcessor = new DefaultPacketProcessor(serializeConfig);
        serializeProcessor.RegisterMessageType<PingMessage>();

        var deserializeProcessor = new DefaultPacketProcessor(deserializeConfig);
        deserializeProcessor.RegisterMessageType<PingMessage>();

        var message = new PingMessage();
        var packetBytes = await serializeProcessor.SerializeAsync(message);

        // Act & Assert
        Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(async () =>
        {
            await deserializeProcessor.DeserializeAsync<PingMessage>(packetBytes);
        });
    }

    [Test]
    public async Task Serialize_WithCompression_ShouldSetCompressedFlag()
    {
        // Arrange
        _networkConfig.CompressionType = CompressionType.GZip;
        var message = new PingMessage();

        // Act
        var packetBytes = await _processor.SerializeAsync(message);
        var packet = MemoryPackSerializer.Deserialize<SquidCraftPacket>(packetBytes);

        // Assert
        Assert.That(packet.FlagType.HasFlag(NetworkMessageFlagType.Compressed), Is.True);
    }

    [Test]
    public async Task Serialize_WithEncryption_ShouldSetEncryptedFlag()
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(EncryptionType.AES256);
        _networkConfig.EncryptionType = EncryptionType.AES256;
        _networkConfig.EncryptionKey = Convert.ToBase64String(key);
        var message = new PingMessage();

        // Act
        var packetBytes = await _processor.SerializeAsync(message);
        var packet = MemoryPackSerializer.Deserialize<SquidCraftPacket>(packetBytes);

        // Assert
        Assert.That(packet.FlagType.HasFlag(NetworkMessageFlagType.Encrypted), Is.True);
    }

    [Test]
    public async Task Serialize_WithCompressionAndEncryption_ShouldSetBothFlags()
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(EncryptionType.AES256);
        _networkConfig.CompressionType = CompressionType.LZ4;
        _networkConfig.EncryptionType = EncryptionType.AES256;
        _networkConfig.EncryptionKey = Convert.ToBase64String(key);
        var message = new PingMessage();

        // Act
        var packetBytes = await _processor.SerializeAsync(message);
        var packet = MemoryPackSerializer.Deserialize<SquidCraftPacket>(packetBytes);

        // Assert
        Assert.That(packet.FlagType.HasFlag(NetworkMessageFlagType.Compressed), Is.True);
        Assert.That(packet.FlagType.HasFlag(NetworkMessageFlagType.Encrypted), Is.True);
    }

    [Test]
    public async Task Serialize_WithoutCompressionOrEncryption_ShouldHaveNoFlags()
    {
        // Arrange
        var message = new PingMessage();

        // Act
        var packetBytes = await _processor.SerializeAsync(message);
        var packet = MemoryPackSerializer.Deserialize<SquidCraftPacket>(packetBytes);

        // Assert
        Assert.That(packet.FlagType, Is.EqualTo(NetworkMessageFlagType.None));
    }
}
