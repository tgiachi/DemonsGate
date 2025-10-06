namespace DemonsGate.Services.Data.Scripts;

public class ScriptResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object? Data { get; set; }
}

public class ScriptResultBuilder
{
    private object? _data;
    private string _message = string.Empty;
    private bool _success;

    public static ScriptResultBuilder CreateSuccess()
    {
        return new ScriptResultBuilder().WithSuccess(true);
    }

    public static ScriptResultBuilder CreateError()
    {
        return new ScriptResultBuilder().WithSuccess(false);
    }

    public ScriptResultBuilder WithSuccess(bool success)
    {
        _success = success;
        return this;
    }

    public ScriptResultBuilder WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public ScriptResultBuilder WithData(object? data)
    {
        _data = data;
        return this;
    }

    public ScriptResultBuilder Success()
    {
        _success = true;
        return this;
    }

    public ScriptResultBuilder Failure()
    {
        _success = false;
        return this;
    }

    public ScriptResult Build()
    {
        return new ScriptResult
        {
            Success = _success,
            Message = _message,
            Data = _data
        };
    }
}
