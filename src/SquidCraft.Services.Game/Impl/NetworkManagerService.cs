using System.Collections.Concurrent;
using SquidCraft.Network.Args;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Interfaces.Services;
using SquidCraft.Network.Messages.Handshake;
using SquidCraft.Network.Messages.Pings;
using SquidCraft.Services.Game.Data.Sessions;
using SquidCraft.Services.Game.Interfaces;
using SquidCraft.Services.Interfaces;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace SquidCraft.Services.Game.Impl;

public class NetworkManagerService : INetworkManagerService
{
    private readonly ILogger _logger = Log.ForContext<NetworkManagerService>();

    private readonly INetworkService _networkService;


    private readonly IEventLoopService _eventLoopService;

    private readonly ObjectPool<PlayerNetworkSession> _playerNetworkSessionPool =
        new DefaultObjectPool<PlayerNetworkSession>(new DefaultPooledObjectPolicy<PlayerNetworkSession>());

    private ConcurrentDictionary<int, PlayerNetworkSession> _sessions = new();
    private readonly ConcurrentBag<Func<PlayerNetworkSession, ISquidCraftMessage, Task>> _listeners = [];
    private bool _isRunning;


    public NetworkManagerService(
        INetworkService networkService, IEventLoopService eventLoopService, ITimerService timerService
    )
    {
        _networkService = networkService;
        _eventLoopService = eventLoopService;
        timerService.RegisterTimerAsync("pingClient", 10 * 1000, OnPingClients, 0, true);
        timerService.RegisterTimerAsync("disconnectDeadClients", 15 * 1000, DisconnectDeadClients, 0, true);
        _networkService.AddMessageListener<PongMessage>(OnPongMessage);
    }

    private Task OnPongMessage(int sessionId, ISquidCraftMessage message)
    {
        if (_sessions.TryGetValue(sessionId, out var player))
        {
            _logger.Debug("Updating ping to {Player}", player.SessionId);
            player.LastPing = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    private async Task DisconnectDeadClients()
    {
        var now = DateTime.UtcNow;
        var timeoutThreshold = now.AddSeconds(-60);

        foreach (var (clientId, session) in _sessions)
        {
            if (session.LastPing <= timeoutThreshold)
            {
                await _networkService.DisconnectClientAsync(clientId);
            }
        }
    }

    private async Task OnPingClients()
    {
        _logger.Information("Pinging clients");
        var now = DateTime.UtcNow;
        var timeoutThreshold = now.AddSeconds(-30);
        var pingMessage = new PingMessage();

        foreach (var (clientId, session) in _sessions)
        {
            if (session.LastPing <= timeoutThreshold)
            {
                await _networkService.SendMessageAsync(clientId, pingMessage);
            }
        }
    }

    private void OnNetworkMessageReceived(object sender, NetworkClientMessageEventArgs e)
    {
        var session = GetOrCreateSession(e.ClientId);

        _eventLoopService.EnqueueTask(
            $"HandleNetworkMessage_{e.ClientId}_{e.Message.MessageType}",
            () => DispatchMessageAsync(session, e.Message)
        );
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            return Task.CompletedTask;
        }

        _networkService.ClientConnected += OnClientConnected;
        _networkService.ClientDisconnected += OnClientDisconnected;
        _networkService.ClientMessageReceived += OnNetworkMessageReceived;

        _isRunning = true;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return Task.CompletedTask;
        }

        _networkService.ClientConnected -= OnClientConnected;
        _networkService.ClientDisconnected -= OnClientDisconnected;
        _networkService.ClientMessageReceived -= OnNetworkMessageReceived;

        foreach (var sessionId in _sessions.Keys)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.Dispose();
                session.SessionId = 0;
                _playerNetworkSessionPool.Return(session);
            }
        }

        _sessions = new ConcurrentDictionary<int, PlayerNetworkSession>();
        _isRunning = false;


        return Task.CompletedTask;
    }

    public void AddListener(Func<PlayerNetworkSession, ISquidCraftMessage, Task> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
        _logger.Debug("Added listener to NetworkManagerService. Total listeners: {ListenerCount}", _listeners.Count);
    }

    public PlayerNetworkSession? GetSessionById(int id)
    {
        return _sessions[id];
    }

    public async Task SendMessages(PlayerNetworkSession session, params ISquidCraftMessage[] messages)
    {
        foreach (var message in messages)
        {
            _eventLoopService.EnqueueTask(
                $"network_send_message_{message.MessageType}_{session.SessionId}",
                () => _networkService.SendMessageAsync(session.SessionId, message)
            );
        }
    }

    private PlayerNetworkSession GetOrCreateSession(int clientId)
    {
        return _sessions.GetOrAdd(
            clientId,
            id =>
            {
                var session = _playerNetworkSessionPool.Get();
                session.SessionId = id;
                session.LastPing = DateTime.UtcNow;

                session.NetworkManagerService = this;

                _logger.Debug("Created session for client {ClientId}", id);
                return session;
            }
        );
    }

    private void OnClientConnected(object sender, NetworkClientConnectedEventArgs e)
    {
        var session = GetOrCreateSession(e.ClientId);
        _logger.Information("Client connected with ID {ClientId}", session.SessionId);
        SendMessages(session, new VersionResponse()
        {
            Version = "0.1.0"
        });
    }

    private void OnClientDisconnected(object sender, NetworkClientConnectedEventArgs e)
    {
        if (_sessions.TryRemove(e.ClientId, out var session))
        {
            session.Dispose();
            session.SessionId = 0;
            _playerNetworkSessionPool.Return(session);

            _logger.Information("Client disconnected with ID {ClientId}", e.ClientId);
        }
        else
        {
            _logger.Warning("Disconnect event received for unknown client {ClientId}", e.ClientId);
        }
    }

    private async Task DispatchMessageAsync(PlayerNetworkSession session, ISquidCraftMessage message)
    {
        if (_listeners.IsEmpty)
        {
            _logger.Warning(
                "Received message of type {MessageType} for client {ClientId} but no listeners are registered",
                message.MessageType,
                session.SessionId
            );
            return;
        }

        foreach (var listener in _listeners)
        {
            try
            {
                await listener(session, message);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Listener {Listener} threw an exception while handling message type {MessageType} for client {ClientId}",
                    listener.Method.Name,
                    message.MessageType,
                    session.SessionId
                );
            }
        }
    }
}
