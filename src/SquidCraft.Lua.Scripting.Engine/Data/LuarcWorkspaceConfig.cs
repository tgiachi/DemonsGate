using System.Text.Json.Serialization;

namespace SquidCraft.Lua.Scripting.Engine.Data;

/// <summary>
///     Workspace configuration for Lua Language Server
/// </summary>
public class LuarcWorkspaceConfig
{
    [JsonPropertyName("library")] public string[] Library { get; set; } = [];

    [JsonPropertyName("checkThirdParty")] public bool CheckThirdParty { get; set; } = false;
}
