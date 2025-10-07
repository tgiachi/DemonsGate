using System.Text.Json.Serialization;
using DemonsGate.Services.Data.Config;
using DemonsGate.Services.Data.Config.Options;
using DemonsGate.Services.Data.Config.Sections;
using DemonsGate.Services.Data.Internal.Diagnostic;

namespace DemonsGate.Services.Context;

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
