namespace SquidCraft.Services.Data.Internal.Events.Dispatcher;

/// <summary>
///     Base class for event dispatch jobs
/// </summary>
public abstract class EventDispatchJob
{
    public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}
