using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Network.Args;
using DemonsGate.Network.Interfaces.Listeners;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Network.Types;

namespace DemonsGate.Network.Interfaces.Services;

/// <summary>
/// Defines the interface for a network client service.
/// </summary>
public interface INetworkClientService : IDemonsGateStartableService
{
    /// <summary>
    /// Delegate for handling connection events.
    /// </summary>
    delegate void NetworkConnectedHandler(object sender, EventArgs e);

    /// <summary>
    /// Delegate for handling disconnection events.
    /// </summary>
    delegate void NetworkDisconnectedHandler(object sender, EventArgs e);

    /// <summary>
    /// Delegate for handling received messages.
    /// </summary>
    delegate void NetworkMessageReceivedHandler(object sender, NetworkClientMessageEventArgs e);

    /// <summary>
    /// Delegate for handling raw message events.
    /// </summary>
    delegate void NetworkRawMessageReceivedHandler(object sender, NetworkClientRawMessageArgs e);

    /// <summary>
    /// Delegate for handling raw message sent events.
    /// </summary>
    delegate void NetworkRawMessageSentHandler(object sender, NetworkClientRawMessageArgs e);

    /// <summary>
    /// Event raised when connected to the server.
    /// </summary>
    event NetworkConnectedHandler? Connected;

    /// <summary>
    /// Event raised when disconnected from the server.
    /// </summary>
    event NetworkDisconnectedHandler? Disconnected;

    /// <summary>
    /// Event raised when a message is received from the server.
    /// </summary>
    event NetworkMessageReceivedHandler? MessageReceived;

    /// <summary>
    /// Event raised when a raw message is received.
    /// </summary>
    event NetworkRawMessageReceivedHandler? RawMessageReceived;

    /// <summary>
    /// Event raised when a raw message is sent.
    /// </summary>
    event NetworkRawMessageSentHandler? RawMessageSent;

    /// <summary>
    /// Gets whether the client is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to a server.
    /// </summary>
    /// <param name="host">The server host.</param>
    /// <param name="port">The server port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> ConnectAsync(string host, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the server.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IDemonsGateMessage;

    /// <summary>
    /// Adds a message listener for a specific message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="listener">The listener to add.</param>
    void AddMessageListener(NetworkMessageType messageType, INetworkMessageListener listener);

    /// <summary>
    /// Adds a message listener for a specific message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="listener">The listener to add.</param>
    void AddMessageListener<TMessage>(INetworkMessageListener listener)
        where TMessage : IDemonsGateMessage;
}
