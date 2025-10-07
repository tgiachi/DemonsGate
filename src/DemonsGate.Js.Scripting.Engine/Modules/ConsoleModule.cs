using System.Globalization;
using DemonsGate.Core.Attributes.Scripts;
using Serilog;

namespace DemonsGate.Js.Scripting.Engine.Modules;

/// <summary>
///     JavaScript console API implementation (console.log, console.error, etc.)
/// </summary>
[ScriptModule("console", "JavaScript-style console API for logging and debugging")]
public class ConsoleModule
{
    private readonly ILogger _logger = Serilog.Log.ForContext<ConsoleModule>();

    // Required to avoid ambiguity with Serilog.Log static class

    public ConsoleModule()
    {
    }

    [ScriptFunction("log")]
    public void Log(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[JS Console] {Message}", message);
    }

    [ScriptFunction("info")]
    public void Info(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Information("[JS Console] ℹ️  {Message}", message);
    }

    [ScriptFunction("warn")]
    public void Warn(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Warning("[JS Console] ⚠️  {Message}", message);
    }

    [ScriptFunction("error")]
    public void Error(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Error("[JS Console] ❌ {Message}", message);
    }

    [ScriptFunction("debug")]
    public void Debug(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        _logger.Debug("[JS Console] 🐛 {Message}", message);
    }

    [ScriptFunction("trace")]
    public void Trace(params object[] args)
    {
        var message = string.Join(" ", args.Select(FormatArg));
        var stackTrace = Environment.StackTrace;
        _logger.Debug("[JS Console] 📍 {Message}\n{StackTrace}", message, stackTrace);
    }

    [ScriptFunction("clear")]
    public void Clear()
    {
        _logger.Information("[JS Console] Console cleared");
        // In a real implementation, this could clear the console component
    }

    [ScriptFunction("assert")]
    public void Assert(bool condition, params object[] args)
    {
        if (!condition)
        {
            var message = args.Length > 0 ? string.Join(" ", args.Select(FormatArg)) : "Assertion failed";
            _logger.Error("[JS Console] ❌ Assertion failed: {Message}", message);
        }
    }

    [ScriptFunction("time")]
    public void Time(string label)
    {
        // Store timer start time (would need dictionary in real implementation)
        _logger.Debug("[JS Console] ⏱️  Timer '{Label}' started", label);
    }

    [ScriptFunction("timeEnd")]
    public void TimeEnd(string label)
    {
        // Calculate elapsed time (would need dictionary in real implementation)
        _logger.Debug("[JS Console] ⏱️  Timer '{Label}' ended", label);
    }

    [ScriptFunction("count")]
    public void Count(string label = "default")
    {
        // Increment counter (would need dictionary in real implementation)
        _logger.Debug("[JS Console] 🔢 Count '{Label}'", label);
    }

    [ScriptFunction("group")]
    public void Group(params object[] args)
    {
        var message = args.Length > 0 ? string.Join(" ", args.Select(FormatArg)) : "Group";
        _logger.Information("[JS Console] 📁 Group: {Message}", message);
    }

    [ScriptFunction("groupEnd")]
    public void GroupEnd()
    {
        _logger.Information("[JS Console] 📁 Group end");
    }

    [ScriptFunction("table")]
    public void Table(object data)
    {
        var formatted = FormatArg(data);
        _logger.Information("[JS Console] 📊 Table:\n{Data}", formatted);
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
