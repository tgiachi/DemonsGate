using System.Globalization;
using Serilog.Events;

namespace Spectra.Engine.Services.Data.Internal.Events.Logger;

/// <summary>
///     Represents log event data that can be emitted via the event bus
/// </summary>
public record LogEventData
{
    /// <summary>
    ///     The timestamp when the log event occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    ///     The log level (Verbose, Debug, Information, Warning, Error, Fatal)
    /// </summary>
    public LogEventLevel Level { get; init; }

    /// <summary>
    ///     The log message template
    /// </summary>
    public string MessageTemplate { get; init; } = string.Empty;

    /// <summary>
    ///     The rendered log message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    ///     The source context (usually the class name)
    /// </summary>
    public string SourceContext { get; init; } = string.Empty;

    /// <summary>
    ///     Additional properties from the log event
    /// </summary>
    public IReadOnlyDictionary<string, LogEventPropertyValue> Properties { get; init; } =
        new Dictionary<string, LogEventPropertyValue>();

    /// <summary>
    ///     Exception information if present
    /// </summary>
    public string? Exception { get; init; }

    /// <summary>
    ///     Creates a LogEventData from a Serilog LogEvent
    /// </summary>
    public static LogEventData FromLogEvent(Serilog.Events.LogEvent logEvent)
    {
        return new LogEventData
        {
            Timestamp = logEvent.Timestamp,
            Level = logEvent.Level,
            MessageTemplate = logEvent.MessageTemplate.Text,
            Message = logEvent.RenderMessage(CultureInfo.CurrentCulture),
            SourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sourceContext)
                ? sourceContext.ToString().Trim('"')
                : string.Empty,
            Properties = logEvent.Properties,
            Exception = logEvent.Exception?.ToString()
        };
    }
}
