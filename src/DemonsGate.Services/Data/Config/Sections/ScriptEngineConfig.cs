using DemonsGate.Services.Types;

namespace DemonsGate.Services.Data.Config.Sections;

/// <summary>
/// public class ScriptEngineConfig.
/// </summary>
public class ScriptEngineConfig
{

    public string DefinitionPath { get; set; } = "scripts";

    public ScriptNameConversion ScriptNameConversion { get; set; } = ScriptNameConversion.CamelCase;

    public List<string> InitScriptsFileNames { get; set; } = ["bootstrap.lua", "main.lua", "init.lua"];


    /// <summary>
    ///     Enable script caching for improved performance (default: true)
    /// </summary>
    public bool EnableScriptCaching { get; set; } = true;


}
