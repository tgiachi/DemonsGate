using SquidCraft.Core.Interfaces.Services;

namespace SquidCraft.Services.Interfaces;

/// <summary>
/// public interface ITimerService : ISquidCraftService.
/// </summary>
public interface ITimerService : ISquidCraftService
{
    string RegisterTimer(string name, double intervalInMs, Action callback, double delayInMs = 0, bool repeat = false);

    string RegisterTimer(string name, TimeSpan interval, Action callback, TimeSpan delay = default, bool repeat = false);

    string RegisterTimerAsync(string name, double intervalInMs, Func<Task> callback, double delayInMs = 0, bool repeat = false);

    string RegisterTimerAsync(string name, TimeSpan interval, Func<Task> callback, TimeSpan delay = default, bool repeat = false);

    void UnregisterTimer(string timerId);

    void UnregisterAllTimers();
}
