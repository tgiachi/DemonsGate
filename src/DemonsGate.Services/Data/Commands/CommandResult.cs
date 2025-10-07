namespace DemonsGate.Services.Data.Commands;

/// <summary>
/// public class CommandResult.
/// </summary>
public class CommandResult
{
    public Exception? Exception { get; set; }
    public string Output { get; set; }
    public bool Success => Exception == null;
}
