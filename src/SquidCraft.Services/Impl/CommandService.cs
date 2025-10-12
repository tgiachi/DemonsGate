using System.Text;
using SquidCraft.Core.Enums;
using SquidCraft.Services.Data.Commands;
using SquidCraft.Services.Interfaces;
using SquidCraft.Services.Types;
using Serilog;

namespace SquidCraft.Services.Impl;

/// <summary>
/// Implements the command service for executing and registering commands.
/// </summary>
public class CommandService : ICommandService
{
    private readonly List<CommandRegistration> _registeredCommands = new();

    private readonly ILogger _logger = Log.ForContext<CommandService>();

    public async Task<CommandResult> ExecuteCommandAsync(string command, CommandSourceType sourceType, int sourceId)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or whitespace.", nameof(command));
        }

        var args = SplitArgs(command).ToArray();
        if (args.Length == 0)
        {
            return new CommandResult(new Exception("No command provided."));
        }

        var commandName = args[0];
        var commandArgs = args.Skip(1).ToArray();

        var registration = _registeredCommands.FirstOrDefault(c =>
            c.Command.Equals(commandName, StringComparison.OrdinalIgnoreCase)
            && (c.AllowedSources & sourceType) != 0
        );
        if (registration == null)
        {
            _logger.Warning(
                "Command not found or not allowed from source. Command: {Command}, Source: {Source}",
                commandName,
                sourceType
            );

            return new CommandResult(new Exception($"Command '{commandName}' not found or not allowed from source."));
        }

        // Here you would typically check the user's level from a user service or database.
        // For this example, we'll assume all users are of UserLevelType.User.
        var userLevel = UserLevelType.User; // This should be fetched based on sourceId.

        if (sourceId == -1)
        {
            userLevel = UserLevelType.All;
        }

        if (userLevel < registration.MinimumUserLevel)
        {
            _logger.Warning(
                "Insufficient permissions for command. Command: {Command}, Required: {Required}, Actual: {Actual}",
                commandName,
                registration.MinimumUserLevel,
                userLevel
            );
            return new CommandResult(new Exception($"Insufficient permissions to execute '{commandName}'."));
        }

        var commandRequest = new CommandRequest(commandName, commandArgs, sourceType, sourceId);
        return await registration.Handler(commandRequest);
    }

    public void RegisterCommand(
        string command, CommandSourceType sourceType, UserLevelType userLevelType,
        Func<CommandRequest, Task<CommandResult>> handler
    )
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or whitespace.", nameof(command));
        }

        ArgumentNullException.ThrowIfNull(handler);

        var registration = new CommandRegistration
        {
            Command = command,
            AllowedSources = sourceType,
            MinimumUserLevel = userLevelType,
            Handler = handler
        };

        _logger.Information(
            "Registering command {Command} from {Source} auth: {UserLevel}",
            command,
            sourceType,
            userLevelType
        );
        _registeredCommands.Add(registration);
    }


    private static IEnumerable<string> SplitArgs(string commandLine)
    {
        var inQuotes = false;
        var current = new StringBuilder();
        foreach (var ch in commandLine)
        {
            if (ch == '\"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }
}
