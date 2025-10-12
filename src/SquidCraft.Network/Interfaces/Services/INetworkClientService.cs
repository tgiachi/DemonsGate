using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Network.Args;
using SquidCraft.Network.Interfaces.Listeners;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Interfaces.Services;

/// <summary>
/// Defines the interface for a network client service.
/// </summary>
public interface INetworkClientService : ISquidCraftStartableService
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
        where TMessage : ISquidCraftMessage;

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
        where TMessage : ISquidCraftMessage;

    /// <summary>
    /// Sends a request message and waits for the corresponding response.
    /// A unique RequestId is automatically generated and assigned to the request.
    /// </summary>
    /// <typeparam name="TRequest">The type of request message</typeparam>
    /// <typeparam name="TResponse">The type of response message</typeparam>
    /// <param name="request">The request message to send</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response message</returns>
    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
        where TRequest : ISquidCraftMessage
        where TResponse : ISquidCraftMessage;

    /// <summary>
    /// Sends a ping request and waits for a pong response.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The pong response with latency information</returns>
    Task<SquidCraft.Network.Messages.Pings.PongMessage> PingAsync(
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a login request and waits for a login response.
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The login response indicating success or failure</returns>
    Task<SquidCraft.Network.Messages.Auth.LoginResponseMessage> LoginAsync(
        string email,
        string password,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the server version information.
    /// </summary>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The version response from the server</returns>
    Task<SquidCraft.Network.Messages.Handshake.VersionResponse> GetVersionAsync(
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a specific asset from the server.
    /// </summary>
    /// <param name="fileName">The name of the asset file to request</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 10000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The asset response containing the requested asset data</returns>
    Task<SquidCraft.Network.Messages.Assets.AssetResponseMessage> RequestAssetAsync(
        string fileName,
        int timeoutMs = 10000,
        CancellationToken cancellationToken = default);
}
