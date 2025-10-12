using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Network.Types;
using SquidCraft.Services.Game.Data.Sessions;

namespace SquidCraft.Services.Game.Interfaces;

public interface INetworkManagerService : ISquidCraftStartableService
{

    delegate void PlayerSessionAddedHandler(PlayerNetworkSession session);
    delegate void PlayerSessionRemovedHandler(PlayerNetworkSession session);
    event PlayerSessionAddedHandler PlayerSessionAdded;

    event PlayerSessionRemovedHandler PlayerSessionRemoved;



    /// <summary>
    /// Adds a listener for all message types.
    /// </summary>
    /// <param name="listener">The function to invoke when any message is received.</param>
    void AddListener(Func<PlayerNetworkSession, ISquidCraftMessage, Task> listener);

    /// <summary>
    /// Adds a listener for a specific message type.
    /// </summary>
    /// <param name="messageType">The type of message to listen for.</param>
    /// <param name="listener">The function to invoke when a message of the specified type is received.</param>
    void AddListener(NetworkMessageType messageType, Func<PlayerNetworkSession, ISquidCraftMessage, Task> listener);

    PlayerNetworkSession? GetSessionById(int id);

    Task SendMessages(PlayerNetworkSession session, params ISquidCraftMessage[] messages);


}
