using SquidCraft.Core.Interfaces.EventLoop;
using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Services.Metrics.EventLoop;
using SquidCraft.Services.Types;

namespace SquidCraft.Services.Interfaces;

/// <summary>
/// Defines a service that manages an event loop for executing actions with different priorities.
/// </summary>
public interface IEventLoopService :  IEventLoopTickDispatcher, ISquidCraftStartableService
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
    /// Enqueues an async task to be executed with normal priority.
    /// </summary>
    /// <param name="name">The name of the task for identification.</param>
    /// <param name="task">The async task to execute.</param>
    /// <returns>The ID of the queued task.</returns>
    string EnqueueTask(string name, Func<Task> task);

    /// <summary>
    /// Enqueues an async task to be executed with the specified priority.
    /// </summary>
    /// <param name="name">The name of the task for identification.</param>
    /// <param name="task">The async task to execute.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The ID of the queued task.</returns>
    string EnqueueTask(string name, Func<Task> task, EventLoopPriority priority);

    /// <summary>
    /// Enqueues an action to be executed after the specified delay with normal priority.
    /// </summary>
    /// <param name="name">The name of the action for identification.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="delay">The delay before executing the action.</param>
    /// <returns>The ID of the queued action.</returns>
    string EnqueueDelayedAction(string name, Action action, TimeSpan delay);

    /// <summary>
    /// Enqueues an async task to be executed after the specified delay with normal priority.
    /// </summary>
    /// <param name="name">The name of the task for identification.</param>
    /// <param name="task">The async task to execute.</param>
    /// <param name="delay">The delay before executing the task.</param>
    /// <returns>The ID of the queued task.</returns>
    string EnqueueDelayedTask(string name, Func<Task> task, TimeSpan delay);

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
    /// Enqueues an async task to be executed after the specified delay with the specified priority.
    /// </summary>
    /// <param name="name">The name of the task for identification.</param>
    /// <param name="task">The async task to execute.</param>
    /// <param name="delay">The delay before executing the task.</param>
    /// <param name="priority">The priority of the task.</param>
    /// <returns>The ID of the queued task.</returns>
    string EnqueueDelayedTask(string name, Func<Task> task, TimeSpan delay, EventLoopPriority priority);

    /// <summary>
    /// Tries to cancel a previously enqueued action.
    /// </summary>
    /// <param name="actionId">The ID of the action to cancel.</param>
    /// <returns>True if the action was found and canceled; otherwise, false.</returns>
    bool TryCancelAction(string actionId);
}
