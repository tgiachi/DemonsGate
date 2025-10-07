using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Services.Data.Commands;
using DemonsGate.Services.Types;

namespace DemonsGate.Services.Interfaces;

/// <summary>
/// Defines the contract for the command service.
/// </summary>
public interface ICommandService : IDemonsGateService
{
    Task<CommandResult> ExecuteCommandAsync(string command, CommandSourceType sourceType, int sourceId);

    void RegisterCommand(
        string command, CommandSourceType sourceType, UserLevelType userLevelType,
        Func<CommandRequest, Task<CommandResult>> handler
    );
}
