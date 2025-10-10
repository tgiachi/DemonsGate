using System.Collections.Concurrent;
using DemonsGate.Network.Args;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Interfaces.Services;
using DemonsGate.Services.Game.Data.Sessions;
using DemonsGate.Services.Game.Interfaces;
using DemonsGate.Services.Interfaces;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace DemonsGate.Services.Game.Impl;

public class NetworkManagerService : INetworkManagerService
{
    private readonly ILogger _logger = Log.ForContext<NetworkManagerService>();

    private readonly INetworkService _networkService;

    private readonly IEventLoopService _eventLoopService;

    private readonly ObjectPool<PlayerNetworkSession> _playerNetworkSessionPool =
        new DefaultObjectPool<PlayerNetworkSession>(new DefaultPooledObjectPolicy<PlayerNetworkSession>());


    private ConcurrentDictionary<int, PlayerNetworkSession> _sessions = new();
    private readonly ConcurrentBag<Func<PlayerNetworkSession, IDemonsGateMessage, Task>> _listeners = new();
    private bool _isRunning;


    public NetworkManagerService(INetworkService networkService, IEventLoopService eventLoopService)
    {
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        _eventLoopService = eventLoopService ?? throw new ArgumentNullException(nameof(eventLoopService));
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

    public void AddListener(Func<PlayerNetworkSession, IDemonsGateMessage, Task> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        _listeners.Add(listener);
        _logger.Debug("Added listener to NetworkManagerService. Total listeners: {ListenerCount}", _listeners.Count);
    }

    private PlayerNetworkSession GetOrCreateSession(int clientId)
    {
        return _sessions.GetOrAdd(
            clientId,
            id =>
            {
                var session = _playerNetworkSessionPool.Get();
                session.SessionId = id;

                _logger.Debug("Created session for client {ClientId}", id);
                return session;
            }
        );
    }

    private void OnClientConnected(object sender, NetworkClientConnectedEventArgs e)
    {
        var session = GetOrCreateSession(e.ClientId);
        _logger.Information("Client connected with ID {ClientId}", session.SessionId);
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

    private async Task DispatchMessageAsync(PlayerNetworkSession session, IDemonsGateMessage message)
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
