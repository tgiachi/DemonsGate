using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Network.Interfaces.Messages;
using DemonsGate.Services.Game.Data.Sessions;

namespace DemonsGate.Services.Game.Interfaces;

public interface INetworkManagerService : IDemonsGateStartableService
{
    void AddListener(Func<PlayerNetworkSession, IDemonsGateMessage, Task> listener);

    PlayerNetworkSession? GetSessionById(int id);


}
