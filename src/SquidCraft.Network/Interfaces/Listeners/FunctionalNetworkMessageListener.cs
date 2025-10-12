using SquidCraft.Network.Interfaces.Messages;

namespace SquidCraft.Network.Interfaces.Listeners;

/// <summary>
/// A functional wrapper around INetworkMessageListener that accepts a Func delegate.
/// </summary>
public sealed class FunctionalNetworkMessageListener : INetworkMessageListener
{
    private readonly Func<int, ISquidCraftMessage, Task> _handler;

    /// <summary>
    /// Creates a new instance of FunctionalNetworkMessageListener with the specified handler function.
    /// </summary>
    /// <param name="handler">The function to invoke when a message is received.</param>
    public FunctionalNetworkMessageListener(Func<int, ISquidCraftMessage, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
    }

    /// <inheritdoc />
    public Task HandleMessageAsync(int sessionId, ISquidCraftMessage message)
    {
        return _handler(sessionId, message);
    }
}
