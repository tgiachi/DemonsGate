using DemonsGate.Services.Types;

namespace DemonsGate.Services.Data.Config.Sections;

/// <summary>
/// public class ScriptEngineConfig.
/// </summary>
public class ScriptEngineConfig
{

    public string DefinitionPath { get; set; } = "scripts";

    public ScriptNameConversion ScriptNameConversion { get; set; } = ScriptNameConversion.CamelCase;

    public List<string> InitScriptsFileNames { get; set; } = ["bootstrap.js", "main.js", "init.js"];

    /// <summary>
    ///     Maximum memory limit for script execution in bytes (default: 4MB)
    /// </summary>
    public int MaxMemoryBytes { get; set; } = 4_000_000;

    /// <summary>
    ///     Script execution timeout in seconds (default: 10s)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    ///     Maximum number of statements a script can execute (default: 10,000)
    /// </summary>
    public int MaxStatements { get; set; } = 10_000;

    /// <summary>
    ///     Enable debug mode for script execution (default: false)
    /// </summary>
    public bool EnableDebugMode { get; set; }

    /// <summary>
    ///     Enable script caching for improved performance (default: true)
    /// </summary>
    public bool EnableScriptCaching { get; set; } = true;

    /// <summary>
    ///     Enable source map support for TypeScript debugging (default: false)
    /// </summary>
    public bool EnableSourceMaps { get; set; }

    /// <summary>
    ///     Path to source maps directory (default: "scripts/maps")
    /// </summary>
    public string SourceMapsPath { get; set; } = "scripts/maps";
}
