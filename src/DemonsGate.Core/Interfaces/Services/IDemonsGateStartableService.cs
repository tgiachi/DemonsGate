namespace DemonsGate.Core.Interfaces.Services;

public interface IDemonsGateStartableService : IDemonsGateService
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
