using SquidCraft.Core.Enums;
using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Services.Data.Commands;
using SquidCraft.Services.Types;

namespace SquidCraft.Services.Interfaces;

/// <summary>
/// Defines the contract for the command service.
/// </summary>
public interface ICommandService : ISquidCraftService
{
    Task<CommandResult> ExecuteCommandAsync(string command, CommandSourceType sourceType, int sourceId);

    void RegisterCommand(
        string command, CommandSourceType sourceType, UserLevelType userLevelType,
        Func<CommandRequest, Task<CommandResult>> handler
    );
}
