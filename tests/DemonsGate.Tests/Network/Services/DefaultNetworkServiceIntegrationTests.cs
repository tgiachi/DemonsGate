using DemonsGate.Core.Enums;
using DemonsGate.Network.Data.Config;
using DemonsGate.Network.Data.Services;
using DemonsGate.Network.Interfaces.Processors;
using DemonsGate.Network.Messages;
using DemonsGate.Network.Services;
using DemonsGate.Network.Types;
using LiteNetLib;
using LiteNetLib.Utils;
using NSubstitute;

namespace DemonsGate.Tests.Network.Services;

[TestFixture]
public class DefaultNetworkServiceIntegrationTests
{
    private IPacketSerializer _mockSerializer = null!;
    private IPacketDeserializer _mockDeserializer = null!;
    private DefaultNetworkService _server = null!;
    private NetManager _client = null!;
    private EventBasedNetListener _clientListener = null!;
    private const int TestPort = 9999;
    private const string TestHost = "127.0.0.1";

    [SetUp]
    public void SetUp()
    {
        _mockSerializer = Substitute.For<IPacketSerializer>();
        _mockDeserializer = Substitute.For<IPacketDeserializer>();

        var networkConfig = new NetworkConfig
        {
            Port = TestPort,
            CompressionType = CompressionType.None,
            EncryptionType = EncryptionType.None,
            EncryptionKey = string.Empty
        };

        var registeredMessages = new List<NetworkMessageData>
        {
            new NetworkMessageData(typeof(PingMessage), NetworkMessageType.Ping)
        };

        _server = new DefaultNetworkService(
            _mockSerializer,
            _mockDeserializer,
            registeredMessages,
            networkConfig
        );

        _clientListener = new EventBasedNetListener();
        _client = new NetManager(_clientListener)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UpdateTime = 15
        };
    }

    [TearDown]
    public async Task TearDown()
    {
        _client?.Stop();
        await _server.StopAsync();
    }

    [Test]
    public async Task Server_ShouldAcceptClientConnection()
    {
        // Arrange
        await _server.StartAsync();
        _client.Start();

        var connectionTcs = new TaskCompletionSource<bool>();
        _clientListener.PeerConnectedEvent += peer =>
        {
            connectionTcs.TrySetResult(true);
        };

        // Act
        _client.Connect(TestHost, TestPort, string.Empty);

        // Poll both server and client
        for (int i = 0; i < 100 && !connectionTcs.Task.IsCompleted; i++)
        {
            _client.PollEvents();
            await Task.Delay(10);
        }

        // Assert
        var connected = await Task.WhenAny(connectionTcs.Task, Task.Delay(2000)) == connectionTcs.Task;
        Assert.That(connected, Is.True, "Client should connect to server");
    }

    [Test]
    public async Task Server_ShouldTriggerClientConnectedEvent()
    {
        // Arrange
        await _server.StartAsync();
        _client.Start();

        var serverEventTcs = new TaskCompletionSource<int>();
        _server.ClientConnected += (sender, args) =>
        {
            serverEventTcs.TrySetResult(args.ClientId);
        };

        // Act
        _client.Connect(TestHost, TestPort, string.Empty);

        // Poll both server and client
        for (int i = 0; i < 100 && !serverEventTcs.Task.IsCompleted; i++)
        {
            _client.PollEvents();
            await Task.Delay(10);
        }

        // Assert
        var eventTriggered = await Task.WhenAny(serverEventTcs.Task, Task.Delay(2000)) == serverEventTcs.Task;
        Assert.That(eventTriggered, Is.True, "Server should trigger ClientConnected event");
    }

    [Test]
    public async Task Server_ShouldSendMessageToClient()
    {
        // Arrange
        await _server.StartAsync();
        _client.Start();

        var messageReceivedTcs = new TaskCompletionSource<byte[]>();
        var clientConnectedTcs = new TaskCompletionSource<int>();

        _server.ClientConnected += (sender, args) =>
        {
            clientConnectedTcs.TrySetResult(args.ClientId);
        };

        _clientListener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            var data = reader.GetBytesWithLength();
            messageReceivedTcs.TrySetResult(data);
        };

        // Mock serializer to return test data
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        _mockSerializer.SerializeAsync(Arg.Any<PingMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testData));

        // Act
        _client.Connect(TestHost, TestPort, string.Empty);

        // Poll until connected
        for (int i = 0; i < 100 && !clientConnectedTcs.Task.IsCompleted; i++)
        {
            _client.PollEvents();
            await Task.Delay(10);
        }

        var clientId = await clientConnectedTcs.Task;
        await _server.SendMessageAsync(clientId, new PingMessage());

        // Poll until message received
        for (int i = 0; i < 100 && !messageReceivedTcs.Task.IsCompleted; i++)
        {
            _client.PollEvents();
            await Task.Delay(10);
        }

        // Assert
        var messageReceived = await Task.WhenAny(messageReceivedTcs.Task, Task.Delay(2000)) == messageReceivedTcs.Task;
        Assert.That(messageReceived, Is.True, "Client should receive message from server");

        if (messageReceived)
        {
            var receivedData = await messageReceivedTcs.Task;
            Assert.That(receivedData, Is.EqualTo(testData), "Received data should match sent data");
        }
    }

    [Test]
    public async Task Server_ShouldBroadcastMessageToMultipleClients()
    {
        // Arrange
        await _server.StartAsync();

        var client1Listener = new EventBasedNetListener();
        var client1 = new NetManager(client1Listener) { AutoRecycle = true, UpdateTime = 15 };
        var client2Listener = new EventBasedNetListener();
        var client2 = new NetManager(client2Listener) { AutoRecycle = true, UpdateTime = 15 };

        var message1ReceivedTcs = new TaskCompletionSource<byte[]>();
        var message2ReceivedTcs = new TaskCompletionSource<byte[]>();
        var clientsConnectedCount = 0;
        var clientsConnectedTcs = new TaskCompletionSource<bool>();

        _server.ClientConnected += (sender, args) =>
        {
            clientsConnectedCount++;
            if (clientsConnectedCount == 2)
            {
                clientsConnectedTcs.TrySetResult(true);
            }
        };

        client1Listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            var data = reader.GetBytesWithLength();
            message1ReceivedTcs.TrySetResult(data);
        };

        client2Listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
        {
            var data = reader.GetBytesWithLength();
            message2ReceivedTcs.TrySetResult(data);
        };

        // Mock serializer
        var testData = new byte[] { 10, 20, 30 };
        _mockSerializer.SerializeAsync(Arg.Any<PingMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(testData));

        // Act
        client1.Start();
        client2.Start();
        client1.Connect(TestHost, TestPort, string.Empty);
        client2.Connect(TestHost, TestPort, string.Empty);

        // Poll until both clients connected
        for (int i = 0; i < 100 && !clientsConnectedTcs.Task.IsCompleted; i++)
        {
            client1.PollEvents();
            client2.PollEvents();
            await Task.Delay(10);
        }

        await _server.BroadcastMessageAsync(new PingMessage());

        // Poll until messages received
        for (int i = 0; i < 100 && (!message1ReceivedTcs.Task.IsCompleted || !message2ReceivedTcs.Task.IsCompleted); i++)
        {
            client1.PollEvents();
            client2.PollEvents();
            await Task.Delay(10);
        }

        // Assert
        var message1Received = await Task.WhenAny(message1ReceivedTcs.Task, Task.Delay(2000)) == message1ReceivedTcs.Task;
        var message2Received = await Task.WhenAny(message2ReceivedTcs.Task, Task.Delay(2000)) == message2ReceivedTcs.Task;

        Assert.That(message1Received, Is.True, "Client 1 should receive broadcast");
        Assert.That(message2Received, Is.True, "Client 2 should receive broadcast");

        // Cleanup
        client1.Stop();
        client2.Stop();
    }

    [Test]
    public async Task Server_ShouldHandleClientDisconnection()
    {
        // Arrange
        await _server.StartAsync();
        _client.Start();

        var clientConnectedTcs = new TaskCompletionSource<int>();
        var clientDisconnectedTcs = new TaskCompletionSource<int>();

        _server.ClientConnected += (sender, args) =>
        {
            clientConnectedTcs.TrySetResult(args.ClientId);
        };

        _server.ClientDisconnected += (sender, args) =>
        {
            clientDisconnectedTcs.TrySetResult(args.ClientId);
        };

        // Act
        _client.Connect(TestHost, TestPort, string.Empty);

        // Poll until connected
        for (int i = 0; i < 100 && !clientConnectedTcs.Task.IsCompleted; i++)
        {
            _client.PollEvents();
            await Task.Delay(10);
        }

        var clientId = await clientConnectedTcs.Task;
        await _server.DisconnectClientAsync(clientId);

        // Poll until disconnected
        for (int i = 0; i < 100 && !clientDisconnectedTcs.Task.IsCompleted; i++)
        {
            _client.PollEvents();
            await Task.Delay(10);
        }

        // Assert
        var disconnected = await Task.WhenAny(clientDisconnectedTcs.Task, Task.Delay(2000)) == clientDisconnectedTcs.Task;
        Assert.That(disconnected, Is.True, "Server should trigger ClientDisconnected event");
    }
}
