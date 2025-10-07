using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Network.Args;
using DemonsGate.Network.Interfaces.Messages;

namespace DemonsGate.Network.Interfaces.Services;

/// <summary>
/// public interface INetworkService : IDemonsGateStartableService.
/// </summary>
public interface INetworkService : IDemonsGateStartableService
{

    delegate void NetworkClientConnectedHandler(object sender, NetworkClientConnectedEventArgs e);
    delegate void NetworkClientDisconnectedHandler(object sender, NetworkClientConnectedEventArgs e);

    delegate void NetworkClientMessageHandler(object sender, NetworkClientMessageEventArgs e);

    delegate void NetworkClientRawMessageReceivedHandler(object sender, NetworkClientRawMessageArgs e);
    delegate void NetworkClientRawMessageSentHandler(object sender, NetworkClientRawMessageArgs e);

    delegate IEnumerable<IDemonsGateMessage> NetworkClientConnectedMessages(int peerId);

    event NetworkClientRawMessageReceivedHandler? ClientRawMessageReceived;
    event NetworkClientRawMessageSentHandler? ClientRawMessageSent;
    event NetworkClientConnectedHandler? ClientConnected;
    event NetworkClientDisconnectedHandler? ClientDisconnected;
    event NetworkClientMessageHandler? ClientMessageReceived;

    event NetworkClientConnectedMessages? ClientConnectedHelloMessages;

    Task SendMessageAsync<TMessage>(int clientId, TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IDemonsGateMessage;

    Task BroadcastMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IDemonsGateMessage;

    Task DisconnectClientAsync(int clientId, CancellationToken cancellationToken = default);


}
