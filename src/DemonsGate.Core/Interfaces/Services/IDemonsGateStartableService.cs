namespace DemonsGate.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for a startable Demons Gate service.
/// </summary>
public interface IDemonsGateStartableService : IDemonsGateService
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
