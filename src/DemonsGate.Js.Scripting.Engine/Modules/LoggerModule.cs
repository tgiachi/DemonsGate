using DemonsGate.Core.Attributes.Scripts;
using Serilog;
using Serilog.Events;

namespace DemonsGate.Js.Scripting.Engine.Modules;

[ScriptModule("logger", "Log messages with different severity levels")]
public class LoggerModule
{
    private readonly ILogger _logger = Log.ForContext<LoggerModule>();

    [ScriptFunction("Logs a verbose message.")]
    public void LogVerbose(string message, object[]? data = null)
    {
        _logger.Verbose(message, data);
    }

    [ScriptFunction("Logs a debug message.")]
    public void LogDebug(string message, object[]? data = null)
    {
        _logger.Debug(message, data);
    }

    [ScriptFunction("Logs an info message.")]
    public void LogInfo(string message, object[]? data = null)
    {
        _logger.Information(message, data);
    }

    [ScriptFunction("Logs a warning message.")]
    public void LogWarning(string message, object[]? data = null)
    {
        _logger.Warning(message, data);
    }

    [ScriptFunction("Logs an error message.")]
    public void LogError(string message, object[]? data = null)
    {
        _logger.Error(message, data);
    }

    [ScriptFunction("Logs a fatal message.")]
    public void LogFatal(string message, object[]? data = null)
    {
        _logger.Fatal(message, data);
    }

    [ScriptFunction("Logs a message with the specified level.")]
    public void LogMessage(LogEventLevel level, string message, object[]? data = null)
    {
        _logger.Write(level, message, data);
    }
}
