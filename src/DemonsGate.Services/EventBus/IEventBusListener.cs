namespace DemonsGate.Services.EventBus;

/// <summary>
/// public interface IEventBusListener<in TEvent>.
/// </summary>
public interface IEventBusListener<in TEvent>
{
    /// <summary>
    ///     Handles the received event.
    /// </summary>
    /// <param name="evt">The event to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(TEvent evt, CancellationToken cancellationToken = default);
}
