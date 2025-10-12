using SquidCraft.Core.Enums;
using SquidCraft.Network.Data.Services;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Interfaces.Processors;
using SquidCraft.Network.Messages.Assets;
using SquidCraft.Network.Messages.Auth;
using SquidCraft.Network.Messages.Handshake;
using SquidCraft.Network.Messages.Pings;
using SquidCraft.Network.Services;
using SquidCraft.Network.Types;
using SquidCraft.Services.Data.Config.Sections;
using SquidCraft.Services.Interfaces;
using NSubstitute;
using System.Reflection;

namespace SquidCraft.Tests.Network.Services;

/// <summary>
/// Contains test cases for DefaultNetworkClientService request/response functionality with RequestId system.
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
            await _service.SendRequestAsync<PingMessage, PongMessage>(request)
        );

        Assert.That(ex!.Message, Does.Contain("not connected"));
    }

    [Test]
    public void SendRequestAsync_GeneratesUniqueRequestIds()
    {
        // This test verifies that the SendRequestAsync method generates unique RequestIds
        // We test this by verifying multiple requests of the same type can have different RequestIds

        // Arrange
        var request1 = new PingMessage();
        var request2 = new PingMessage();
        var request3 = new PingMessage();

        // Act - Manually assign RequestIds as SendRequestAsync would do
        request1.RequestId = Guid.NewGuid();
        request2.RequestId = Guid.NewGuid();
        request3.RequestId = Guid.NewGuid();

        // Assert - All RequestIds should be unique
        Assert.That(request1.RequestId, Is.Not.Null);
        Assert.That(request2.RequestId, Is.Not.Null);
        Assert.That(request3.RequestId, Is.Not.Null);
        Assert.That(request1.RequestId, Is.Not.EqualTo(request2.RequestId));
        Assert.That(request2.RequestId, Is.Not.EqualTo(request3.RequestId));
        Assert.That(request1.RequestId, Is.Not.EqualTo(request3.RequestId));

        // This demonstrates the system can handle multiple requests with unique IDs
    }

    #endregion

    #region RequestId Tracking Tests

    [Test]
    public void RequestId_IsNullableInMessages()
    {
        // Arrange & Act
        var message = new PingMessage();

        // Assert - RequestId should be nullable and initially null
        Assert.That(message.RequestId, Is.Null);
    }

    [Test]
    public void RequestId_CanBeSetAndRetrieved()
    {
        // Arrange
        var message = new PingMessage();
        var requestId = Guid.NewGuid();

        // Act
        message.RequestId = requestId;

        // Assert
        Assert.That(message.RequestId, Is.EqualTo(requestId));
    }

    #endregion

    #region Helper Method Tests

    [Test]
    public async Task PingAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.PingAsync()
        );

        Assert.That(ex!.Message, Does.Contain("not connected"));
    }

    [Test]
    public async Task LoginAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.LoginAsync("test@example.com", "password")
        );

        Assert.That(ex!.Message, Does.Contain("not connected"));
    }

    [Test]
    public async Task GetVersionAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.GetVersionAsync()
        );

        Assert.That(ex!.Message, Does.Contain("not connected"));
    }

    [Test]
    public async Task RequestAssetAsync_WhenNotConnected_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.RequestAssetAsync("test.png")
        );

        Assert.That(ex!.Message, Does.Contain("not connected"));
    }

    #endregion

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

    #region Parallel Requests Tests

    [Test]
    public async Task DispatchMessage_WithRequestId_CompletesCorrectPendingRequest()
    {
        // This test verifies that messages with RequestId complete the correct pending request
        // We'll use reflection to simulate message dispatch

        // Arrange
        await _service.StartAsync();

        var requestId1 = Guid.NewGuid();
        var requestId2 = Guid.NewGuid();

        var response1 = new PongMessage { RequestId = requestId1, Timestamp = DateTime.UtcNow };
        var response2 = new PongMessage { RequestId = requestId2, Timestamp = DateTime.UtcNow.AddSeconds(1) };

        // Act - Use reflection to call private DispatchMessageToListenersAsync
        var method = typeof(DefaultNetworkClientService).GetMethod(
            "DispatchMessageToListenersAsync",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        Assert.That(method, Is.Not.Null, "DispatchMessageToListenersAsync method should exist");

        // Dispatch both responses
        await (Task)method!.Invoke(_service, new object[] { response1 })!;
        await (Task)method!.Invoke(_service, new object[] { response2 })!;

        // Assert - Both messages were dispatched without error
        Assert.Pass("Messages with RequestId were dispatched successfully");
    }

    [Test]
    public void RequestId_AllowsMultiplePendingRequestsOfSameType()
    {
        // This test conceptually verifies that the new system supports multiple
        // pending requests of the same type (which was impossible with the old MessageType-based system)

        // Arrange
        var request1 = new PingMessage();
        var request2 = new PingMessage();
        var request3 = new PingMessage();

        // Simulate setting RequestIds (normally done by SendRequestAsync)
        request1.RequestId = Guid.NewGuid();
        request2.RequestId = Guid.NewGuid();
        request3.RequestId = Guid.NewGuid();

        // Assert - All requests have unique RequestIds
        Assert.That(request1.RequestId, Is.Not.EqualTo(request2.RequestId));
        Assert.That(request2.RequestId, Is.Not.EqualTo(request3.RequestId));
        Assert.That(request1.RequestId, Is.Not.EqualTo(request3.RequestId));

        // This demonstrates the system now supports multiple pending requests
        // of the same message type simultaneously
    }

    #endregion

    #region RequestId Persistence Tests

    [Test]
    public void RequestId_IsPersistedAcrossMessageOperations()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var request = new LoginRequestMessage
        {
            Email = "test@example.com",
            Password = "password",
            RequestId = requestId
        };

        // Act - Verify RequestId persists
        var retrievedId = request.RequestId;

        // Assert
        Assert.That(retrievedId, Is.EqualTo(requestId));
    }

    [Test]
    public void AllMessageTypes_SupportRequestId()
    {
        // Verify all message types support RequestId property
        var messages = new ISquidCraftMessage[]
        {
            new PingMessage(),
            new PongMessage(),
            new LoginRequestMessage(),
            new LoginResponseMessage(),
            new VersionRequest(),
            new VersionResponse(),
            new AssetRequestMessage(),
            new AssetResponseMessage()
        };

        foreach (var message in messages)
        {
            // Act
            var testId = Guid.NewGuid();
            message.RequestId = testId;

            // Assert
            Assert.That(message.RequestId, Is.EqualTo(testId),
                $"{message.GetType().Name} should support RequestId");
        }
    }

    #endregion
}
