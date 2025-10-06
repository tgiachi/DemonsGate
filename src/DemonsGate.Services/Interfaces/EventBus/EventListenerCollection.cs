using System.Runtime.CompilerServices;
using DemonsGate.Services.EventBus;

namespace DemonsGate.Services.Interfaces.EventBus;

/// <summary>
///     High-performance thread-safe listener collection optimized for fast add/remove operations
/// </summary>
internal sealed class EventListenerCollection<TEvent> : IEventListenerCollection
    where TEvent : class
{
    private readonly Lock _lock = new();
    private volatile List<IEventBusListener<TEvent>> _listeners = new();

    public int Count => _listeners.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(IEventBusListener<TEvent> listener)
    {
        lock (_lock)
        {
            var newList = new List<IEventBusListener<TEvent>>(_listeners) { listener };
            _listeners = newList;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(IEventBusListener<TEvent> listener)
    {
        lock (_lock)
        {
            var currentList = _listeners;
            var index = currentList.IndexOf(listener);
            if (index >= 0)
            {
                var newList = new List<IEventBusListener<TEvent>>(currentList);
                newList.RemoveAt(index);
                _listeners = newList;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveHandler(Func<TEvent, Task> handler)
    {
        lock (_lock)
        {
            var currentList = _listeners;
            var newList = new List<IEventBusListener<TEvent>>();

            foreach (var listener in currentList)
            {
                if (listener is not FunctionSignalListener<TEvent> funcListener ||
                    !funcListener.HasSameHandler(handler))
                {
                    newList.Add(listener);
                }
            }

            _listeners = newList;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<IEventBusListener<TEvent>> GetListeners()
    {
        return _listeners; // Safe to return volatile reference for read-only operations
    }
}
