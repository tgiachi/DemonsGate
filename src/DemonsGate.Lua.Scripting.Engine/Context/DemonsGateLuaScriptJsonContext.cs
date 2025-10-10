using System.Text.Json.Serialization;
using DemonsGate.Lua.Scripting.Engine.Data;

namespace DemonsGate.Lua.Scripting.Engine.Context;

[JsonSerializable(typeof(LuarcConfig))]
[JsonSerializable(typeof(LuarcRuntimeConfig))]
[JsonSerializable(typeof(LuarcWorkspaceConfig))]
[JsonSerializable(typeof(LuarcDiagnosticsConfig))]
[JsonSerializable(typeof(LuarcCompletionConfig))]
[JsonSerializable(typeof(LuarcFormatConfig))]
public partial class DemonsGateLuaScriptJsonContext : JsonSerializerContext
{
}
