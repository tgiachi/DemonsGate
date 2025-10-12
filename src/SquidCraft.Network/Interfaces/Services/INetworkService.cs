using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Network.Args;
using SquidCraft.Network.Interfaces.Listeners;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Types;

namespace SquidCraft.Network.Interfaces.Services;

/// <summary>
/// public interface INetworkService : ISquidCraftStartableService.
/// </summary>
public interface INetworkService : ISquidCraftStartableService
{


    delegate void NetworkClientConnectedHandler(object sender, NetworkClientConnectedEventArgs e);
    delegate void NetworkClientDisconnectedHandler(object sender, NetworkClientConnectedEventArgs e);
    delegate void NetworkClientMessageHandler(object sender, NetworkClientMessageEventArgs e);

    delegate void NetworkClientRawMessageReceivedHandler(object sender, NetworkClientRawMessageArgs e);
    delegate void NetworkClientRawMessageSentHandler(object sender, NetworkClientRawMessageArgs e);

    delegate IEnumerable<ISquidCraftMessage> NetworkClientConnectedMessages(int peerId);

    event NetworkClientRawMessageReceivedHandler? ClientRawMessageReceived;
    event NetworkClientRawMessageSentHandler? ClientRawMessageSent;
    event NetworkClientConnectedHandler? ClientConnected;
    event NetworkClientDisconnectedHandler? ClientDisconnected;
    event NetworkClientMessageHandler? ClientMessageReceived;

    event NetworkClientConnectedMessages? ClientConnectedHelloMessages;

    void AddMessageListener(NetworkMessageType messageType, INetworkMessageListener listener);

    void AddMessageListener<TMessage>(INetworkMessageListener listener)
        where TMessage : ISquidCraftMessage;

    void AddMessageListener<TMessage>(Func<int, ISquidCraftMessage, Task> handler)
        where TMessage : ISquidCraftMessage;

    Task SendMessageAsync<TMessage>(int clientId, TMessage message, CancellationToken cancellationToken = default)
        where TMessage : ISquidCraftMessage;

    Task BroadcastMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : ISquidCraftMessage;

    Task DisconnectClientAsync(int clientId, CancellationToken cancellationToken = default);


}
