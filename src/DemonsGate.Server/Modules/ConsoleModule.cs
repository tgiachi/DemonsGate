using System.Globalization;
using DemonsGate.Core.Attributes.Scripts;
using Serilog;

namespace DemonsGate.Server.Modules;

/// <summary>
///     Console API implementation (console.log, console.error, etc.)
/// </summary>
[ScriptModule("console", "Console API for logging and debugging")]
public class ConsoleModule
{
    private readonly ILogger _logger = Serilog.Log.ForContext<ConsoleModule>();

    public ConsoleModule()
    {
    }

    [ScriptFunction(functionName: "log")]
    public void Log(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[Console] {Message}", message);
    }

    [ScriptFunction(functionName: "info")]
    public void Info(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[Console] {Message}", message);
    }

    [ScriptFunction(functionName: "warn")]
    public void Warn(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Warning("[Console] {Message}", message);
    }

    [ScriptFunction(functionName: "error")]
    public void Error(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Error("[Console] {Message}", message);
    }

    [ScriptFunction(functionName: "debug")]
    public void Debug(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Debug("[Console] {Message}", message);
    }

    [ScriptFunction(functionName: "trace")]
    public void Trace(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        var stackTrace = Environment.StackTrace;
        _logger.Debug("[Console] {Message}\n{StackTrace}", message, stackTrace);
    }

    [ScriptFunction(functionName: "clear")]
    public void Clear()
    {
        _logger.Information("[Console] Console cleared");
        // In a real implementation, this could clear the console component
    }

    [ScriptFunction(functionName: "assert")]
    public void Assert(bool condition, params object[] args)
    {
        if (!condition)
        {
            var message = args.Length > 0 ? string.Join(" ", args.Select(FormatArg)) : "Assertion failed";
            _logger.Error("[Console] Assertion failed: {Message}", message);
        }
    }

    [ScriptFunction(functionName: "time")]
    public void Time(string label)
    {
        // Store timer start time (would need dictionary in real implementation)
        _logger.Debug("[Console] Timer '{Label}' started", label);
    }

    [ScriptFunction(functionName: "timeEnd")]
    public void TimeEnd(string label)
    {
        // Calculate elapsed time (would need dictionary in real implementation)
        _logger.Debug("[Console] Timer '{Label}' ended", label);
    }

    [ScriptFunction(functionName: "count")]
    public void Count(string label = "default")
    {
        // Increment counter (would need dictionary in real implementation)
        _logger.Debug("[Console] Count '{Label}'", label);
    }

    [ScriptFunction(functionName: "group")]
    public void Group(params object[] args)
    {
        var message = args.Length > 0 ? string.Join(" ", args.Select(FormatArg)) : "Group";
        _logger.Information("[Console] Group: {Message}", message);
    }

    [ScriptFunction(functionName: "groupEnd")]
    public void GroupEnd()
    {
        _logger.Information("[Console] Group end");
    }

    [ScriptFunction(functionName: "table")]
    public void Table(object data)
    {
        var formatted = FormatArg(data);
        _logger.Information("[Console] Table:\n{Data}", formatted);
    }

    private static string FormatArg(object? arg)
    {
        if (arg == null)
        {
            return "null";
        }

        if (arg is string str)
        {
            return str;
        }

        if (arg is bool b)
        {
            return b.ToString().ToLower(CultureInfo.InvariantCulture);
        }

        return arg.ToString() ?? "undefined";
    }
}
