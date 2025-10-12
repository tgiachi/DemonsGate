using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using SquidCraft.Services.Data.Internal.Events.Dispatcher;
using SquidCraft.Services.EventBus;
using SquidCraft.Services.Interfaces;
using SquidCraft.Services.Interfaces.EventBus;
using Serilog;

namespace SquidCraft.Services.Impl;

/// <summary>
///     High-performance event bus service with async processing and reactive extensions
///     Uses channels for optimal throughput and memory efficiency
/// </summary>
public class EventBusService : IEventBusService, IDisposable
{
    // Unbounded channel for high throughput event processing
    private readonly Channel<EventDispatchJob> _channel;
    private readonly CancellationTokenSource _cts = new();

    // Use ConcurrentDictionary with List<T> instead of ConcurrentBag for better performance
    private readonly ConcurrentDictionary<Type, IEventListenerCollection> _listeners = new();
    private readonly ILogger _logger = Log.ForContext<EventBusService>();
    private readonly Task _processingTask;

    /// <summary>
    ///     Initializes the EventBusService with optimized channel configuration
    /// </summary>
    public EventBusService()
    {
        // Configure channel for maximum performance
        var options = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _channel = Channel.CreateUnbounded<EventDispatchJob>(options);
        AllEventsObservable = new Subject<object>();

        // Start background processing task
        _processingTask = Task.Run(ProcessEventsAsync, _cts.Token);

        _logger.Information("EventBusService initialized with optimized Channel configuration");
    }

    /// <summary>
    ///     Performs clean disposal of resources
    /// </summary>
    public void Dispose()
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }

        try
        {
            _cts.Cancel();
            _channel.Writer.TryComplete();

            // Wait for processing task (non-blocking)
            try
            {
                _processingTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected during cancellation
            }

            AllEventsObservable?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during EventBusService disposal");
        }
        finally
        {
            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    ///     Observable that emits all events for reactive programming
    /// </summary>
    public Subject<object> AllEventsObservable { get; }

    /// <summary>
    ///     Registers a listener for a specific event type with thread-safe operations
    /// </summary>
    /// <typeparam name="TEvent">The event type to listen for</typeparam>
    /// <param name="listener">The listener instance</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subscribe<TEvent>(IEventBusListener<TEvent> listener) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(listener);

        var eventType = typeof(TEvent);
        var listenerCollection = _listeners.GetOrAdd(
            eventType,
            static _ => new EventListenerCollection<TEvent>()
        ) as EventListenerCollection<TEvent>;

        listenerCollection!.Add(listener);

        _logger.Verbose(
            "Registered listener {ListenerType} for event {EventType}",
            listener.GetType().Name,
            eventType.Name
        );
    }

    /// <summary>
    ///     Registers a function as a listener for a specific event type
    /// </summary>
    /// <typeparam name="TEvent">The event type to listen for</typeparam>
    /// <param name="handler">The handler function</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var listener = new FunctionSignalListener<TEvent>(handler);
        Subscribe(listener);

        _logger.Verbose("Registered function handler for event {EventType}", typeof(TEvent).Name);
    }

    /// <summary>
    ///     Unregisters a listener for a specific event type with optimized removal
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="listener">The listener to remove</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unsubscribe<TEvent>(IEventBusListener<TEvent> listener) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(listener);

        var eventType = typeof(TEvent);
        if (_listeners.TryGetValue(eventType, out var listenerCollection))
        {
            var typedCollection = (EventListenerCollection<TEvent>)listenerCollection;
            typedCollection.Remove(listener);

            _logger.Verbose(
                "Unregistered listener {ListenerType} from event {EventType}",
                listener.GetType().Name,
                eventType.Name
            );
        }
    }

    /// <summary>
    ///     Publishes an event to all registered listeners asynchronously with high performance
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="eventData">The event data</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var eventType = typeof(TEvent);

        // Emit to reactive observable
        AllEventsObservable.OnNext(eventData);

        if (!_listeners.TryGetValue(eventType, out var listenerCollection))
        {
            _logger.Verbose("No listeners registered for event {EventType}", eventType.Name);
            return;
        }

        var typedCollection = (EventListenerCollection<TEvent>)listenerCollection;
        var listeners = typedCollection.GetListeners();

        if (listeners.Count == 0)
        {
            _logger.Verbose("No active listeners for event {EventType}", eventType.Name);
            return;
        }

        _logger.Verbose(
            "Publishing event {EventType} to {ListenerCount} listeners",
            eventType.Name,
            listeners.Count
        );

        // Batch write all jobs to channel for maximum throughput
        foreach (var listener in listeners)
        {
            var job = new EventDispatchJob<TEvent>(listener, eventData);
            await _channel.Writer.WriteAsync(job, cancellationToken);
        }
    }

    /// <summary>
    ///     Publishes an event to all registered listeners synchronously (fire-and-forget)
    ///     Uses Task.Run to avoid blocking the caller while still leveraging async processing
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="eventData">The event data</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // Fire-and-forget: don't wait for completion
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await PublishAsync(eventData, _cts.Token);
                }
                catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error in fire-and-forget publish for event {EventType}", typeof(TEvent).Name);
                }
            },
            _cts.Token
        );

        _logger.Verbose("Fire-and-forget publish initiated for event {EventType}", typeof(TEvent).Name);
    }

    /// <summary>
    ///     Returns total listener count across all event types
    /// </summary>
    /// <returns>Total number of registered listeners</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetListenerCount()
    {
        var total = 0;
        foreach (var collection in _listeners.Values)
        {
            total += collection.Count;
        }

        return total;
    }

    /// <summary>
    ///     Returns listener count for a specific event type
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <returns>Number of listeners for the specified event type</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetListenerCount<TEvent>() where TEvent : class
    {
        var eventType = typeof(TEvent);
        return _listeners.TryGetValue(eventType, out var collection) ? collection.Count : 0;
    }

    /// <summary>
    ///     Waits for all pending events to be processed with proper cleanup
    /// </summary>
    /// <returns>Task that completes when all events are processed</returns>
    public async Task WaitForCompletionAsync()
    {
        _channel.Writer.TryComplete();
        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _logger.Debug("Event processing cancelled during completion wait");
        }
    }

    /// <summary>
    ///     Unregisters a function handler for a specific event type
    /// </summary>
    /// <typeparam name="TEvent">The event type</typeparam>
    /// <param name="handler">The handler function to remove</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        if (_listeners.TryGetValue(eventType, out var listenerCollection))
        {
            var typedCollection = (EventListenerCollection<TEvent>)listenerCollection;
            typedCollection.RemoveHandler(handler);

            _logger.Verbose("Unregistered function handler for event {EventType}", eventType.Name);
        }
    }

    /// <summary>
    ///     High-performance background processor for event dispatch jobs
    /// </summary>
    private async Task ProcessEventsAsync()
    {
        try
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    await job.ExecuteAsync(_cts.Token);
                }
                catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
                {
                    // Expected during shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing event dispatch job {JobType}", job.GetType().Name);
                    // Continue processing other jobs
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Event processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error in event processing task");
        }
        finally
        {
            _logger.Debug("Event processing task completed");
        }
    }
}
