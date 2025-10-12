using System.Text.Json.Serialization;
using SquidCraft.Services.Data.Config;
using SquidCraft.Services.Data.Config.Options;
using SquidCraft.Services.Data.Config.Sections;
using SquidCraft.Services.Data.Internal.Diagnostic;

namespace SquidCraft.Services.Context;

/// <summary>
/// Provides JSON serialization context for SquidCraft configuration types.
/// </summary>
[JsonSerializable(typeof(SquidCraftServerConfig))]
[JsonSerializable(typeof(SquidCraftServerOptions))]
[JsonSerializable(typeof(GameNetworkConfig))]
[JsonSerializable(typeof(ScriptEngineConfig))]
[JsonSerializable(typeof(EventLoopConfig))]
[JsonSerializable(typeof(DiagnosticServiceConfig))]
public partial class SquidCraftJsonContext : JsonSerializerContext
{

}
