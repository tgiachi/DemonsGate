using SquidCraft.Network.Interfaces.Messages;

namespace SquidCraft.Network.Interfaces.Listeners;

public interface INetworkMessageListener
{
    Task HandleMessageAsync(int sessionId, ISquidCraftMessage message);

}
