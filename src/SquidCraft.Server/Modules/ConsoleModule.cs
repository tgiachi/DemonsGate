using System.Globalization;
using SquidCraft.Core.Attributes.Scripts;
using Serilog;

namespace SquidCraft.Server.Modules;

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
