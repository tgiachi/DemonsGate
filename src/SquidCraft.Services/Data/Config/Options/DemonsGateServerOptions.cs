using SquidCraft.Services.Types;

namespace SquidCraft.Services.Data.Config.Options;

/// <summary>
/// public class DemonsGateServerOptions.
/// </summary>
public class DemonsGateServerOptions
{
    public string PidFileName { get; set; } = "demonsgate.pid";
    public string ConfigFileName { get; set; } = "demonsgate_server.json";
    public LogLevelType LogLevel { get; set; } = LogLevelType.Information;
    public string RootDirectory { get; set; }
    public bool IsShellEnabled { get; set; } = true;
}
