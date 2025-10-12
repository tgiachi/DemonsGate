using System.Text.Json.Serialization;

namespace SquidCraft.Lua.Scripting.Engine.Data;

/// <summary>
///     Default format configuration for Lua Language Server
/// </summary>
public class LuarcFormatDefaultConfig
{
    [JsonPropertyName("indent_style")] public string IndentStyle { get; set; } = "space";

    [JsonPropertyName("indent_size")] public string IndentSize { get; set; } = "4";
}
