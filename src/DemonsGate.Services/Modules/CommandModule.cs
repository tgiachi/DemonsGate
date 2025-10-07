using DemonsGate.Core.Attributes.Scripts;
using DemonsGate.Services.Data.Commands;
using DemonsGate.Services.Interfaces;
using DemonsGate.Services.Types;


namespace DemonsGate.Services.Modules;

[ScriptModule("commands")]
public class CommandModule
{
    private readonly ICommandService _commandService;

    public CommandModule(ICommandService commandService)
    {
        _commandService = commandService;
    }

    [ScriptFunction("Register a command with the command service.")]
    public void RegisterCommand(
        string command, Func<ScriptExecutionContext, CommandResult> handler,
        CommandSourceType allowedSources = CommandSourceType.All, UserLevelType minimumUserLevel = UserLevelType.User
    )
    {
        _commandService.RegisterCommand(
            command,
            allowedSources,
            minimumUserLevel,
            request =>
            {
                var context = new ScriptExecutionContext()
                {
                    Request = request,
                };
                return Task.FromResult(handler(context));
            }
        );
    }
}

public class ScriptExecutionContext
{
    public CommandResult Result { get; set; }
    public CommandRequest Request { get; set; }

    public CommandResult Ok(string text) => Result = CommandResult.Ok(text);

    public CommandResult Error(string error) => Result = CommandResult.Fail(new Exception(error));
}
