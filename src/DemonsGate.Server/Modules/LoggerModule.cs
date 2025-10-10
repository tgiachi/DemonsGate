using DemonsGate.Core.Attributes.Scripts;
using Serilog;
using Serilog.Events;

namespace DemonsGate.Server.Modules;

/// <summary>
/// Provides logging functionality for scripts.
/// </summary>
[ScriptModule("logger", "Log messages with different severity levels")]
public class LoggerModule
{
    private readonly ILogger _logger = Serilog.Log.ForContext<LoggerModule>();

    public LoggerModule()
    {
    }

    [ScriptFunction(functionName: "verbose", helpText: "Logs a verbose message.")]
    public void LogVerbose(string message, object[]? data = null)
    {
        _logger.Verbose(message, data);
    }

    [ScriptFunction(functionName: "debug", helpText: "Logs a debug message.")]
    public void LogDebug(string message, object[]? data = null)
    {
        _logger.Debug(message, data);
    }

    [ScriptFunction(functionName: "info", helpText: "Logs an info message.")]
    public void LogInfo(string message, object[]? data = null)
    {
        _logger.Information(message, data);
    }

    [ScriptFunction(functionName: "warning", helpText: "Logs a warning message.")]
    public void LogWarning(string message, object[]? data = null)
    {
        _logger.Warning(message, data);
    }

    [ScriptFunction(functionName: "error", helpText: "Logs an error message.")]
    public void LogError(string message, object[]? data = null)
    {
        _logger.Error(message, data);
    }

    [ScriptFunction(functionName: "fatal", helpText: "Logs a fatal message.")]
    public void LogFatal(string message, object[]? data = null)
    {
        _logger.Fatal(message, data);
    }

    [ScriptFunction(functionName: "log", helpText: "Logs a message with the specified level.")]
    public void LogMessage(LogEventLevel level, string message, object[]? data = null)
    {
        _logger.Write(level, message, data);
    }
}
