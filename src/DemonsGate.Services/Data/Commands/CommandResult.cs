namespace DemonsGate.Services.Data.Commands;

/// <summary>
/// public class CommandResult.
/// </summary>
public class CommandResult
{
    public Exception? Exception { get; set; }
    public string Output { get; set; }
    public bool Success => Exception == null;

    public CommandResult(string output)
    {
        Output = output;
    }

    public CommandResult(Exception exception)
    {
        Exception = exception;
        Output = exception.Message;
    }

    public static CommandResult FromOutput(string output) => new CommandResult(output);
}
