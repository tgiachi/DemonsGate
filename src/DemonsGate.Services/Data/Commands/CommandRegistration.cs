using DemonsGate.Services.Types;

namespace DemonsGate.Services.Data.Commands;

/// <summary>
/// public class CommandRegistration.
/// </summary>
public class CommandRegistration
{
    public string Command { get; set; }

    public CommandSourceType AllowedSources { get; set; } = CommandSourceType.All;

    public UserLevelType MinimumUserLevel { get; set; } = UserLevelType.User;

    public Func<CommandRequest, Task<CommandResult>> Handler { get; set; }
}
