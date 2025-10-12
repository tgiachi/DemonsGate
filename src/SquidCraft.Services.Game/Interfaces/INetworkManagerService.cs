using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Network.Interfaces.Messages;
using SquidCraft.Services.Game.Data.Sessions;

namespace SquidCraft.Services.Game.Interfaces;

public interface INetworkManagerService : ISquidCraftStartableService
{
    void AddListener(Func<PlayerNetworkSession, ISquidCraftMessage, Task> listener);

    PlayerNetworkSession? GetSessionById(int id);

    Task SendMessages(PlayerNetworkSession session, params ISquidCraftMessage[] messages);


}
