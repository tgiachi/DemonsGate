namespace SquidCraft.Services.Data.Scripts;

/// <summary>
///     Detailed information about a JavaScript execution error
/// </summary>
public class ScriptErrorInfo
{
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public int? LineNumber { get; set; }
    public int? ColumnNumber { get; set; }
    public string? FileName { get; set; }
    public string? ErrorType { get; set; }
    public string? SourceCode { get; set; }

    /// <summary>
    ///     Original TypeScript file name if source maps are available
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    ///     Original line number in TypeScript if source maps are available
    /// </summary>
    public int? OriginalLineNumber { get; set; }

    /// <summary>
    ///     Original column number in TypeScript if source maps are available
    /// </summary>
    public int? OriginalColumnNumber { get; set; }
}
