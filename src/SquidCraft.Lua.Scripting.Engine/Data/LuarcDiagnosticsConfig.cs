using System.Text.Json.Serialization;

namespace SquidCraft.Lua.Scripting.Engine.Data;

/// <summary>
///     Diagnostics configuration for Lua Language Server
/// </summary>
public class LuarcDiagnosticsConfig
{
    [JsonPropertyName("globals")] public string[] Globals { get; set; } = [];
}
