using System.Collections.Concurrent;
using DemonsGate.Network.Args;
using DemonsGate.Network.Data.Services;
using DemonsGate.Network.Interfaces.Listeners;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Interfaces.Processors;
using DemonsGate.Network.Interfaces.Services;
using DemonsGate.Network.Types;
using DemonsGate.Services.Data.Config.Sections;
using DemonsGate.Services.Interfaces;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace DemonsGate.Network.Services;

/// <summary>
/// Implements the network service for handling client connections and messaging.
/// </summary>
public class DefaultNetworkService : INetworkService
{
    public event INetworkService.NetworkClientRawMessageReceivedHandler? ClientRawMessageReceived;
    public event INetworkService.NetworkClientRawMessageSentHandler? ClientRawMessageSent;
    public event INetworkService.NetworkClientConnectedHandler? ClientConnected;
    public event INetworkService.NetworkClientDisconnectedHandler? ClientDisconnected;
    public event INetworkService.NetworkClientMessageHandler? ClientMessageReceived;
    public event INetworkService.NetworkClientConnectedMessages? ClientConnectedHelloMessages;


    private readonly IEventLoopService _eventLoopService;

    private readonly ILogger _logger = Log.ForContext<DefaultNetworkService>();

    private readonly Dictionary<NetworkMessageType, List<INetworkMessageListener>> _messageListeners = new();

    private readonly ConcurrentDictionary<int, NetPeer> _clients = new();

    private readonly IPacketSerializer _packetSerializer;

    private readonly IPacketDeserializer _packetDeserializer;

    private readonly ObjectPool<NetDataWriter> _writerPool =
        new DefaultObjectPool<NetDataWriter>(new DefaultPooledObjectPolicy<NetDataWriter>());

    private readonly EventBasedNetListener _netListener = new();
    private readonly NetManager? _netManager;

    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    private readonly GameNetworkConfig _networkConfig;

    private readonly List<NetworkMessageData> _registeredMessages;

    public DefaultNetworkService(
        IPacketSerializer packetSerializer, IPacketDeserializer packetDeserializer,
        List<NetworkMessageData> registeredMessages, GameNetworkConfig networkConfig,
        IEventLoopService eventLoopService
    )
    {
        _packetSerializer = packetSerializer;
        _packetDeserializer = packetDeserializer;
        _registeredMessages = registeredMessages;
        _networkConfig = networkConfig;
        _eventLoopService = eventLoopService;

        _netManager = new NetManager(_netListener)
        {
            AutoRecycle = true,
            IPv6Enabled = false,
            UpdateTime = 15,
            DisconnectTimeout = 3000
        };


        _netListener.ConnectionRequestEvent += OnConnectionRequest;
        _netListener.PeerConnectedEvent += OnPeerEvent;
        _netListener.NetworkReceiveEvent += OnMessageReceived;

        RegisterInitialMessages();

        eventLoopService.OnTick += OnEventLoopTick;
    }

    private void OnEventLoopTick(double tickDurationMs)
    {
        _netManager?.PollEvents();
    }

    private async void OnMessageReceived(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        _logger.Debug(
            "Received message from {EndPoint} on channel {Channel} with delivery method {DeliveryMethod}",
            peer.Id,
            channel,
            deliveryMethod
        );
        try
        {
            var messageData = reader.GetBytesWithLength();

            var message = await _packetDeserializer.DeserializeAsync<IDemonsGateMessage>(messageData);

            await DispatchMessageToListenersAsync(peer.Id, message);

            _logger.Debug(
                "Deserialized message of type {MessageType} from {Id}",
                message.MessageType,
                peer.Id
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing message from {EndPoint}", peer.Id);
        }
    }

    private async Task DispatchMessageToListenersAsync(int clientId, IDemonsGateMessage message)
    {
        if (_messageListeners.TryGetValue(message.MessageType, out var listeners))
        {
            foreach (var networkMessageListener in listeners)
            {
                _eventLoopService.EnqueueTask(
                    $"HandleMessage_{message.MessageType}",
                    async () =>
                    {
                        try
                        {
                            await networkMessageListener.HandleMessageAsync(clientId, message);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(
                                ex,
                                "Error in listener {Listener} for message type {MessageType}",
                                networkMessageListener.GetType().Name,
                                message.MessageType
                            );
                        }
                    }
                );
            }
        }

        _logger.Warning("No listeners registered for message type {MessageType}", message.MessageType);
    }

    private void OnPeerEvent(NetPeer peer)
    {
        _logger.Information("Peer connected: {EndPoint}", peer.Id);
        var messageToSend = new List<IDemonsGateMessage>();

        messageToSend.AddRange(ClientConnectedHelloMessages?.Invoke(peer.Id) ?? []);

        foreach (var message in messageToSend)
        {
            _ = SendMessageAsync(peer.Id, message);
        }
    }

    private void OnConnectionRequest(ConnectionRequest request)
    {
        var peer = request.Accept();

        _logger.Information("Accepted connection request from {EndPoint}", request.RemoteEndPoint);

        _clients.TryAdd(peer.Id, peer);

        ClientConnected?.Invoke(this, new NetworkClientConnectedEventArgs(peer.Id));
    }

    private void RegisterInitialMessages()
    {
        // Register initial messages here if needed
        _logger.Information("Registering {MessageCount} initial network messages", _registeredMessages.Count);

        foreach (var message in _registeredMessages)
        {
            _logger.Debug("Registered message type: {MessageType} ({Type})", message.MessageType, message.type);

            _packetDeserializer.RegisterMessageType(message.type, message.MessageType);
        }
    }


    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _netManager?.Start(_networkConfig.Port);
        _logger.Information("Network service started on port {Port}", _networkConfig.Port);

        _pollCts = new CancellationTokenSource();
        _pollTask = Task.Run(
            async () =>
            {
                try
                {
                    while (!_pollCts.Token.IsCancellationRequested)
                    {
                        _netManager?.PollEvents();
                        await Task.Delay(15, _pollCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                }
            },
            _pollCts.Token
        );
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_pollCts != null)
        {
            await _pollCts.CancelAsync();
        }

        if (_pollTask != null)
        {
            try
            {
                await _pollTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }

            _pollTask = null;
        }

        _pollCts?.Dispose();
        _pollCts = null;

        _netManager?.Stop();

        _logger.Information("Network service stopped");
    }


    public void AddMessageListener(NetworkMessageType messageType, INetworkMessageListener listener)
    {
        if (!_messageListeners.TryGetValue(messageType, out List<INetworkMessageListener>? value))
        {
            value = [];
            _messageListeners[messageType] = value;
        }

        value.Add(listener);

        _logger.Debug("Added listener for message type {MessageType}", messageType);
    }

    public void AddMessageListener<TMessage>(INetworkMessageListener listener) where TMessage : IDemonsGateMessage
    {
        var messageData = _registeredMessages.FirstOrDefault(m => m.type == typeof(TMessage));
        if (messageData == null)
        {
            _logger.Error("Message type {MessageType} not registered", typeof(TMessage).Name);
            throw new InvalidOperationException($"Message type {typeof(TMessage).Name} not registered");
        }

        AddMessageListener(messageData.MessageType, listener);
    }

    public void AddMessageListener<TMessage>(Func<int, IDemonsGateMessage, Task> handler) where TMessage : IDemonsGateMessage
    {
        var listener = new FunctionalNetworkMessageListener(handler);
        AddMessageListener<TMessage>(listener);
    }

    public async Task SendMessageAsync<TMessage>(
        int clientId, TMessage message, CancellationToken cancellationToken = default
    )
        where TMessage : IDemonsGateMessage
    {
        if (!_clients.TryGetValue(clientId, out var peer))
        {
            _logger.Warning("Client with ID {ClientId} not found", clientId);
            return;
        }

        var packet = await _packetSerializer.SerializeAsync(message, cancellationToken);

        var writer = _writerPool.Get();
        writer.Reset();
        writer.PutBytesWithLength(packet);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);

        ClientRawMessageSent?.Invoke(this, new NetworkClientRawMessageArgs(clientId, packet));

        _writerPool.Return(writer);

        _logger.Debug("Sent message of type {MessageType} to client {ClientId}", message.MessageType, clientId);
    }

    public Task BroadcastMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IDemonsGateMessage
    {
        var tasks = _clients.Keys.Select(clientId => SendMessageAsync(clientId, message, cancellationToken));
        return Task.WhenAll(tasks);
    }

    public async Task DisconnectClientAsync(int clientId, CancellationToken cancellationToken = default)
    {
        if (_clients.TryRemove(clientId, out var peer))
        {
            peer.Disconnect();
            ClientDisconnected?.Invoke(this, new NetworkClientConnectedEventArgs(clientId));
            _logger.Information("Disconnected client with ID {ClientId}", clientId);
        }
        else
        {
            _logger.Warning("Client with ID {ClientId} not found for disconnection", clientId);
        }
    }
}
