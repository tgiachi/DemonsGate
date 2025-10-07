namespace DemonsGate.Core.Interfaces.Services;

/// <summary>
/// public interface IDemonsGateStartableService : IDemonsGateService.
/// </summary>
public interface IDemonsGateStartableService : IDemonsGateService
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
