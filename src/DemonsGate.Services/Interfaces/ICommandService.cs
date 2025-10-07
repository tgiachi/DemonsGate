using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Services.Data.Commands;
using DemonsGate.Services.Types;

namespace DemonsGate.Services.Interfaces;

public interface ICommandService : IDemonsGateService
{
    Task<CommandResult> ExecuteCommand(string command, CommandSourceType sourceType, int sourceId);


    

}
