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
/// Default implementation of the network client service.
/// </summary>
public class DefaultNetworkClientService : INetworkClientService
{
    public event INetworkClientService.NetworkConnectedHandler? Connected;
    public event INetworkClientService.NetworkDisconnectedHandler? Disconnected;
    public event INetworkClientService.NetworkMessageReceivedHandler? MessageReceived;
    public event INetworkClientService.NetworkRawMessageReceivedHandler? RawMessageReceived;
    public event INetworkClientService.NetworkRawMessageSentHandler? RawMessageSent;

    private readonly IEventLoopService _eventLoopService;
    private readonly ILogger _logger = Log.ForContext<DefaultNetworkClientService>();
    private readonly Dictionary<NetworkMessageType, List<INetworkMessageListener>> _messageListeners = new();
    private readonly IPacketSerializer _packetSerializer;
    private readonly IPacketDeserializer _packetDeserializer;
    private readonly ObjectPool<NetDataWriter> _writerPool =
        new DefaultObjectPool<NetDataWriter>(new DefaultPooledObjectPolicy<NetDataWriter>());

    private readonly EventBasedNetListener _netListener = new();
    private readonly NetManager _netManager;
    private readonly GameNetworkConfig _networkConfig;
    private readonly List<NetworkMessageData> _registeredMessages;

    private NetPeer? _serverPeer;
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    public bool IsConnected => _serverPeer?.ConnectionState == ConnectionState.Connected;

    public DefaultNetworkClientService(
        IPacketSerializer packetSerializer,
        IPacketDeserializer packetDeserializer,
        List<NetworkMessageData> registeredMessages,
        GameNetworkConfig networkConfig,
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

        _netListener.PeerConnectedEvent += OnPeerConnected;
        _netListener.PeerDisconnectedEvent += OnPeerDisconnected;
        _netListener.NetworkReceiveEvent += OnMessageReceived;

        RegisterInitialMessages();

        eventLoopService.OnTick += OnEventLoopTick;
    }

    private void OnEventLoopTick(double tickDurationMs)
    {
        _netManager?.PollEvents();
    }

    private void RegisterInitialMessages()
    {
        _logger.Information("Registering {MessageCount} network messages", _registeredMessages.Count);

        foreach (var message in _registeredMessages)
        {
            _logger.Debug("Registered message type: {MessageType} ({Type})", message.MessageType, message.type);
            _packetDeserializer.RegisterMessageType(message.type, message.MessageType);
        }
    }

    private void OnPeerConnected(NetPeer peer)
    {
        _logger.Information("Connected to server: {PeerId}", peer.Id);
        _serverPeer = peer;
        Connected?.Invoke(this, EventArgs.Empty);
    }

    private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _logger.Information("Disconnected from server: {PeerId}, Reason: {Reason}", peer.Id, disconnectInfo.Reason);
        _serverPeer = null;
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    private async void OnMessageReceived(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
    {
        _logger.Debug(
            "Received message from server on channel {Channel} with delivery method {DeliveryMethod}",
            channel,
            deliveryMethod
        );

        try
        {
            var messageData = reader.GetBytesWithLength();
            RawMessageReceived?.Invoke(this, new NetworkClientRawMessageArgs(0, messageData));

            var message = await _packetDeserializer.DeserializeAsync<IDemonsGateMessage>(messageData);

            await DispatchMessageToListenersAsync(message);

            _logger.Debug("Deserialized message of type {MessageType}", message.MessageType);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing message from server");
        }
    }

    private async Task DispatchMessageToListenersAsync(IDemonsGateMessage message)
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
                            await networkMessageListener.HandleMessageAsync(0, message);
                            MessageReceived?.Invoke(this, new NetworkClientMessageEventArgs(0, message, message.MessageType));
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
        else
        {
            _logger.Warning("No listeners registered for message type {MessageType}", message.MessageType);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _netManager.Start();
        _logger.Information("Network client service started");

        _pollCts = new CancellationTokenSource();
        _pollTask = Task.Run(
            async () =>
            {
                try
                {
                    while (!_pollCts.Token.IsCancellationRequested)
                    {
                        _netManager.PollEvents();
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

        await Task.CompletedTask;
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

        _netManager.Stop();

        _logger.Information("Network client service stopped");
    }

    public async Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _logger.Information("Connecting to {Host}:{Port}", host, port);

        var peer = _netManager.Connect(host, port, string.Empty);
        if (peer == null)
        {
            _logger.Error("Failed to initiate connection to {Host}:{Port}", host, port);
            return false;
        }

        // Wait for connection with timeout
        var timeout = Task.Delay(5000, cancellationToken);
        while (!IsConnected && !cancellationToken.IsCancellationRequested)
        {
            if (await Task.WhenAny(Task.Delay(10, cancellationToken), timeout) == timeout)
            {
                _logger.Warning("Connection timeout to {Host}:{Port}", host, port);
                return false;
            }
        }

        return IsConnected;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_serverPeer != null)
        {
            _serverPeer.Disconnect();
            _logger.Information("Disconnecting from server");
        }

        await Task.CompletedTask;
    }

    public async Task SendMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IDemonsGateMessage
    {
        if (_serverPeer == null || !IsConnected)
        {
            _logger.Warning("Cannot send message: not connected to server");
            return;
        }

        var packet = await _packetSerializer.SerializeAsync(message, cancellationToken);

        var writer = _writerPool.Get();
        writer.Reset();
        writer.PutBytesWithLength(packet);

        _serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);

        RawMessageSent?.Invoke(this, new NetworkClientRawMessageArgs(0, packet));

        _writerPool.Return(writer);

        _logger.Debug("Sent message of type {MessageType} to server", message.MessageType);
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

    public void AddMessageListener<TMessage>(INetworkMessageListener listener)
        where TMessage : IDemonsGateMessage
    {
        var messageData = _registeredMessages.FirstOrDefault(m => m.type == typeof(TMessage));
        if (messageData == null)
        {
            _logger.Error("Message type {MessageType} not registered", typeof(TMessage).Name);
            throw new InvalidOperationException($"Message type {typeof(TMessage).Name} not registered");
        }

        AddMessageListener(messageData.MessageType, listener);
    }
}
