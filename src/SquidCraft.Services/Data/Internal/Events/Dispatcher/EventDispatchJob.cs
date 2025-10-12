using SquidCraft.Services.EventBus;
using Serilog;

namespace SquidCraft.Services.Data.Internal.Events.Dispatcher;

/// <summary>
///     Generic implementation of event dispatch job
/// </summary>
public class EventDispatchJob<TEvent> : EventDispatchJob
    where TEvent : class
{
    private readonly TEvent _event;
    private readonly IEventBusListener<TEvent> _listener;

    private readonly ILogger _logger = Log.ForContext<EventDispatchJob<TEvent>>();

    public EventDispatchJob(IEventBusListener<TEvent> listener, TEvent evt)
    {
        _listener = listener;
        _event = evt;
    }

    public override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _listener.HandleAsync(_event, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing event dispatch job for event type {EventType}", typeof(TEvent).Name);
            throw;
        }
    }
}
