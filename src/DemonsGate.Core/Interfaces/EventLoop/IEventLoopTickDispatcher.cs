namespace DemonsGate.Core.Interfaces.EventLoop;

public interface IEventLoopTickDispatcher
{

    /// <summary>
    ///  Represents a method that will handle the event loop tick event.
    /// </summary>
    delegate void EventLoopTickHandler(double tickDurationMs);

    /// <summary>
    ///  Represents a method that will handle the event loop reset event.
    /// </summary>
    delegate void EventLoopResetHandler();

    /// <summary>
    ///  Gets the current tick count of the event loop.
    /// </summary>
    long TickCount { get; }

    /// <summary>
    ///  Occurs when the event loop ticks.
    /// </summary>
    event EventLoopTickHandler OnTick;

    /// <summary>
    ///  Occurs when the event loop is reset.
    /// </summary>
    event EventLoopResetHandler OnTickReset;
}
