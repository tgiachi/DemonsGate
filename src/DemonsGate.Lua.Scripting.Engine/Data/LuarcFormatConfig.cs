using System.Text.Json.Serialization;

namespace DemonsGate.Lua.Scripting.Engine.Data;

/// <summary>
///     Format configuration for Lua Language Server
/// </summary>
public class LuarcFormatConfig
{
    [JsonPropertyName("enable")] public bool Enable { get; set; } = true;

    [JsonPropertyName("defaultConfig")] public LuarcFormatDefaultConfig DefaultConfig { get; set; } = new();
}
