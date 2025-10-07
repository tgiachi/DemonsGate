using DemonsGate.Services.Data.Commands;
using DemonsGate.Services.Interfaces;
using DemonsGate.Services.Types;

namespace DemonsGate.Services.Impl;

/// <summary>
/// public class CommandService : ICommandService.
/// </summary>
public class CommandService : ICommandService
{
    public Task<CommandResult> ExecuteCommand(string command, CommandSourceType sourceType, int sourceId)
    {
        return null;
    }
}
