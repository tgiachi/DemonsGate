using System.Text.Json.Serialization;
using SquidCraft.Services.Data.Config;
using SquidCraft.Services.Data.Config.Options;
using SquidCraft.Services.Data.Config.Sections;
using SquidCraft.Services.Data.Internal.Diagnostic;

namespace SquidCraft.Services.Context;

/// <summary>
/// Provides JSON serialization context for DemonsGate configuration types.
/// </summary>
[JsonSerializable(typeof(DemonsGateServerConfig))]
[JsonSerializable(typeof(DemonsGateServerOptions))]
[JsonSerializable(typeof(GameNetworkConfig))]
[JsonSerializable(typeof(ScriptEngineConfig))]
[JsonSerializable(typeof(EventLoopConfig))]
[JsonSerializable(typeof(DiagnosticServiceConfig))]
public partial class DemonsGateJsonContext : JsonSerializerContext
{

}
