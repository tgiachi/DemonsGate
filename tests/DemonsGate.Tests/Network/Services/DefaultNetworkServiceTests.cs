using DemonsGate.Core.Enums;
using DemonsGate.Network.Args;
using DemonsGate.Network.Data.Config;
using DemonsGate.Network.Data.Services;
using DemonsGate.Network.Interfaces.Processors;
using DemonsGate.Network.Messages;
using DemonsGate.Network.Services;
using DemonsGate.Network.Types;
using NSubstitute;

namespace DemonsGate.Tests.Network.Services;

[TestFixture]
public class DefaultNetworkServiceTests
{
    private IPacketSerializer _mockSerializer = null!;
    private IPacketDeserializer _mockDeserializer = null!;
    private NetworkConfig _networkConfig = null!;
    private List<NetworkMessageData> _registeredMessages = null!;
    private DefaultNetworkService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSerializer = Substitute.For<IPacketSerializer>();
        _mockDeserializer = Substitute.For<IPacketDeserializer>();
        _networkConfig = new NetworkConfig
        {
            Port = 7777,
            CompressionType = CompressionType.None,
            EncryptionType = EncryptionType.None,
            EncryptionKey = string.Empty
        };
        _registeredMessages = new List<NetworkMessageData>
        {
            new NetworkMessageData(typeof(PingMessage), NetworkMessageType.Ping)
        };

        _service = new DefaultNetworkService(
            _mockSerializer,
            _mockDeserializer,
            _registeredMessages,
            _networkConfig
        );
    }

    [TearDown]
    public async Task TearDown()
    {
        await _service.StopAsync();
    }

    [Test]
    public async Task StartAsync_ShouldStartNetworkService()
    {
        // Act
        await _service.StartAsync();

        // Assert - Service should start without throwing
        Assert.Pass();
    }

    [Test]
    public async Task StopAsync_ShouldStopNetworkService()
    {
        // Arrange
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert - Service should stop without throwing
        Assert.Pass();
    }

    [Test]
    public async Task SendMessageAsync_WithInvalidClientId_ShouldNotThrow()
    {
        // Arrange
        await _service.StartAsync();
        var message = new PingMessage();
        var invalidClientId = 999;

        // Act & Assert - Should not throw, just log warning
        Assert.DoesNotThrowAsync(async () =>
        {
            await _service.SendMessageAsync(invalidClientId, message);
        });
    }

    [Test]
    public async Task DisconnectClientAsync_WithInvalidClientId_ShouldNotThrow()
    {
        // Arrange
        await _service.StartAsync();
        var invalidClientId = 999;

        // Act & Assert - Should not throw, just log warning
        Assert.DoesNotThrowAsync(async () =>
        {
            await _service.DisconnectClientAsync(invalidClientId);
        });
    }

    [Test]
    public async Task BroadcastMessageAsync_WithNoClients_ShouldNotThrow()
    {
        // Arrange
        await _service.StartAsync();
        var message = new PingMessage();

        // Act & Assert - Should not throw when no clients connected
        Assert.DoesNotThrowAsync(async () =>
        {
            await _service.BroadcastMessageAsync(message);
        });
    }

    [Test]
    public void ClientConnected_EventShouldBeSubscribable()
    {
        // Arrange & Act - Verify event can be subscribed to without throwing
        Assert.DoesNotThrow(() =>
        {
            _service.ClientConnected += (sender, args) => { };
        });
    }

    [Test]
    public void ClientDisconnected_EventShouldBeSubscribable()
    {
        // Arrange & Act - Verify event can be subscribed to without throwing
        Assert.DoesNotThrow(() =>
        {
            _service.ClientDisconnected += (sender, args) => { };
        });
    }

    [Test]
    public void ClientRawMessageSent_EventShouldBeSubscribable()
    {
        // Arrange & Act - Verify event can be subscribed to without throwing
        Assert.DoesNotThrow(() =>
        {
            _service.ClientRawMessageSent += (sender, args) => { };
        });
    }

    [Test]
    public void Constructor_ShouldRegisterInitialMessages()
    {
        // Assert - Verify that deserializer's RegisterMessageType was called for each registered message
        _mockDeserializer.Received(_registeredMessages.Count).RegisterMessageType(
            Arg.Any<Type>(),
            Arg.Any<NetworkMessageType>()
        );
    }
}
