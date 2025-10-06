using DemonsGate.Core.Interfaces.EventLoop;
using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Services.Metrics.EventLoop;
using DemonsGate.Services.Types;

namespace DemonsGate.Services.Interfaces;

/// <summary>
/// Defines a service that manages an event loop for executing actions with different priorities.
/// </summary>
public interface IEventLoopService :  IEventLoopTickDispatcher, IDemonsGateStartableService
{

    /// <summary>
    /// Gets or sets the interval in milliseconds between each tick of the event loop.
    /// </summary>
    int TickIntervalMs { get; set; }

    /// <summary>
    /// Gets the current metrics of the event loop.
    /// </summary>
    EventLoopMetrics Metrics { get; }

    /// <summary>
    /// Enqueues an action to be executed with normal priority.
    /// </summary>
    /// <param name="name">The name of the action for identification.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The ID of the queued action.</returns>
    string EnqueueAction(string name, Action action);

    /// <summary>
    /// Enqueues an action to be executed with the specified priority.
    /// </summary>
    /// <param name="name">The name of the action for identification.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The priority of the action.</param>
    /// <returns>The ID of the queued action.</returns>
    string EnqueueAction(string name, Action action, EventLoopPriority priority);

    /// <summary>
    /// Enqueues an action to be executed after the specified delay with normal priority.
    /// </summary>
    /// <param name="name">The name of the action for identification.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="delay">The delay before executing the action.</param>
    /// <returns>The ID of the queued action.</returns>
    string EnqueueDelayedAction(string name, Action action, TimeSpan delay);

    /// <summary>
    ///  Delays the execution of the current thread for the specified number of milliseconds.
    /// </summary>
    /// <param name="milliseconds"></param>
    /// <returns></returns>
    Task Delay(int milliseconds);

    /// <summary>
    /// Enqueues an action to be executed after the specified delay with the specified priority.
    /// </summary>
    /// <param name="name">The name of the action for identification.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="delay">The delay before executing the action.</param>
    /// <param name="priority">The priority of the action.</param>
    /// <returns>The ID of the queued action.</returns>
    string EnqueueDelayedAction(string name, Action action, TimeSpan delay, EventLoopPriority priority);

    /// <summary>
    /// Tries to cancel a previously enqueued action.
    /// </summary>
    /// <param name="actionId">The ID of the action to cancel.</param>
    /// <returns>True if the action was found and canceled; otherwise, false.</returns>
    bool TryCancelAction(string actionId);
}
