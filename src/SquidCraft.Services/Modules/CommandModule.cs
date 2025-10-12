using SquidCraft.Core.Attributes.Scripts;
using SquidCraft.Core.Enums;
using SquidCraft.Services.Data.Commands;
using SquidCraft.Services.Interfaces;
using SquidCraft.Services.Types;


namespace SquidCraft.Services.Modules;

[ScriptModule("commands")]
public class CommandModule
{
    private readonly ICommandService _commandService;

    public CommandModule(ICommandService commandService)
    {
        _commandService = commandService;
    }

    [ScriptFunction(helpText: "Register a command with the command service.")]
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
