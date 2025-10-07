using DemonsGate.Services.Data.Config.Sections;
using DemonsGate.Services.Data.Internal.Diagnostic;

namespace DemonsGate.Services.Data.Config;

/// <summary>
/// public class DemonsGateServerConfig.
/// </summary>
public class DemonsGateServerConfig
{

    public GameNetworkConfig Network { get; set; } = new();

    public ScriptEngineConfig ScriptEngine { get; set; } = new();

    public EventLoopConfig EventLoop { get; set; } = new();

    public DiagnosticServiceConfig Diagnostic { get; set; } = new();

}
