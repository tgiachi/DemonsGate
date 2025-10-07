using DemonsGate.Core.Interfaces.Services;

namespace DemonsGate.Services.Interfaces;

/// <summary>
/// public interface ITimerService : IDemonsGateService.
/// </summary>
public interface ITimerService : IDemonsGateService
{
    string RegisterTimer(string name, double intervalInMs, Action callback, double delayInMs = 0, bool repeat = false);

    void UnregisterTimer(string timerId);

    void UnregisterAllTimers();
}
