namespace SquidCraft.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for a startable Demons Gate service.
/// </summary>
public interface ISquidCraftStartableService : ISquidCraftService
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
