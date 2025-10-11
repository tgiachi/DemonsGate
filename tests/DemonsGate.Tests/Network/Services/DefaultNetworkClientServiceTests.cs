using DemonsGate.Core.Enums;
using DemonsGate.Network.Data.Services;
using DemonsGate.Network.Interfaces.Processors;
using DemonsGate.Network.Messages.Assets;
using DemonsGate.Network.Messages.Auth;
using DemonsGate.Network.Messages.Handshake;
using DemonsGate.Network.Messages.Pings;
using DemonsGate.Network.Services;
using DemonsGate.Network.Types;
using DemonsGate.Services.Data.Config.Sections;
using DemonsGate.Services.Interfaces;
using NSubstitute;

namespace DemonsGate.Tests.Network.Services;

/// <summary>
/// Contains test cases for DefaultNetworkClientService request/response functionality.
/// </summary>
[TestFixture]
public class DefaultNetworkClientServiceTests
{
    private IPacketSerializer _mockSerializer = null!;
    private IPacketDeserializer _mockDeserializer = null!;
    private IEventLoopService _mockEventLoop = null!;
    private GameNetworkConfig _networkConfig = null!;
    private List<NetworkMessageData> _registeredMessages = null!;
    private DefaultNetworkClientService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSerializer = Substitute.For<IPacketSerializer>();
        _mockDeserializer = Substitute.For<IPacketDeserializer>();
        _mockEventLoop = Substitute.For<IEventLoopService>();

        _networkConfig = new GameNetworkConfig
        {
            Port = 7777,
            CompressionType = CompressionType.None,
            EncryptionType = EncryptionType.None,
            EncryptionKey = string.Empty
        };

        _registeredMessages = new List<NetworkMessageData>
        {
            new(typeof(PingMessage), NetworkMessageType.Ping),
            new(typeof(PongMessage), NetworkMessageType.Pong),
            new(typeof(LoginRequestMessage), NetworkMessageType.LoginRequest),
            new(typeof(LoginResponseMessage), NetworkMessageType.LoginResponse),
            new(typeof(VersionRequest), NetworkMessageType.VersionRequest),
            new(typeof(VersionResponse), NetworkMessageType.VersionResponse),
            new(typeof(AssetRequestMessage), NetworkMessageType.AssetRequest),
            new(typeof(AssetResponseMessage), NetworkMessageType.AssetResponse)
        };

        _service = new DefaultNetworkClientService(
            _mockSerializer,
            _mockDeserializer,
            _registeredMessages,
            _networkConfig,
            _mockEventLoop
        );

        // Setup serializer to return empty byte array for any message type
        _mockSerializer.SerializeAsync(Arg.Any<PingMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<byte>()));
        _mockSerializer.SerializeAsync(Arg.Any<LoginRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<byte>()));
        _mockSerializer.SerializeAsync(Arg.Any<VersionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<byte>()));
        _mockSerializer.SerializeAsync(Arg.Any<AssetRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Array.Empty<byte>()));
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_service.IsConnected)
        {
            await _service.DisconnectAsync();
        }
        await _service.StopAsync();
    }

    #region SendRequestAsync Tests

    [Test]
    public async Task SendRequestAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new PingMessage();

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.SendRequestAsync<PingMessage, PongMessage>(
                request,
                NetworkMessageType.Pong,
                timeoutMs: 5000
            )
        );

        Assert.That(ex.Message, Does.Contain("not connected"));
    }

    // Note: Tests for pending requests and timeouts require a real connection
    // These are better suited for integration tests with a real server

    #endregion

    #region PingAsync Tests

    [Test]
    public async Task PingAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.PingAsync()
        );

        Assert.That(ex.Message, Does.Contain("not connected"));
    }


    #endregion

    #region LoginAsync Tests

    [Test]
    public async Task LoginAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.LoginAsync("test@example.com", "password")
        );

        Assert.That(ex.Message, Does.Contain("not connected"));
    }


    #endregion

    #region GetVersionAsync Tests

    [Test]
    public async Task GetVersionAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.GetVersionAsync()
        );

        Assert.That(ex.Message, Does.Contain("not connected"));
    }


    #endregion

    #region RequestAssetAsync Tests

    [Test]
    public async Task RequestAssetAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.RequestAssetAsync("test.png")
        );

        Assert.That(ex.Message, Does.Contain("not connected"));
    }


    #endregion

    // Note: Cancellation token tests require a real connection
    // These are better suited for integration tests

    #region Service Lifecycle Tests

    [Test]
    public async Task Service_CanStartAndStop()
    {
        // Act
        await _service.StartAsync();
        Assert.DoesNotThrowAsync(async () => await _service.StopAsync());
    }

    [Test]
    public async Task Service_IsNotConnectedInitially()
    {
        // Assert
        Assert.That(_service.IsConnected, Is.False);
    }


    #endregion

    // Note: Serialization verification tests require a real connection
    // These are better suited for integration tests

    #region Message Registration Tests

    [Test]
    public void Constructor_RegistersAllMessages()
    {
        // Assert - Verify deserializer was called for each registered message
        _mockDeserializer.Received(1).RegisterMessageType(typeof(PingMessage), NetworkMessageType.Ping);
        _mockDeserializer.Received(1).RegisterMessageType(typeof(PongMessage), NetworkMessageType.Pong);
        _mockDeserializer.Received(1).RegisterMessageType(typeof(LoginRequestMessage), NetworkMessageType.LoginRequest);
        _mockDeserializer.Received(1).RegisterMessageType(typeof(LoginResponseMessage), NetworkMessageType.LoginResponse);
        _mockDeserializer.Received(1).RegisterMessageType(typeof(VersionRequest), NetworkMessageType.VersionRequest);
        _mockDeserializer.Received(1).RegisterMessageType(typeof(VersionResponse), NetworkMessageType.VersionResponse);
        _mockDeserializer.Received(1).RegisterMessageType(typeof(AssetRequestMessage), NetworkMessageType.AssetRequest);
        _mockDeserializer.Received(1).RegisterMessageType(typeof(AssetResponseMessage), NetworkMessageType.AssetResponse);
    }

    #endregion
}
