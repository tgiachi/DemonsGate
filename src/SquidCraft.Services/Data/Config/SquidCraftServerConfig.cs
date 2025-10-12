using SquidCraft.Services.Data.Config.Sections;
using SquidCraft.Services.Data.Internal.Diagnostic;

namespace SquidCraft.Services.Data.Config;

/// <summary>
/// public class SquidCraftServerConfig.
/// </summary>
public class SquidCraftServerConfig
{

    public GameNetworkConfig Network { get; set; } = new();

    public ScriptEngineConfig ScriptEngine { get; set; } = new();

    public EventLoopConfig EventLoop { get; set; } = new();

    public DiagnosticServiceConfig Diagnostic { get; set; } = new();

}
