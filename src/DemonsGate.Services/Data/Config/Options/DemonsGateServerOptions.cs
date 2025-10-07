using DemonsGate.Services.Types;

namespace DemonsGate.Services.Data.Config.Options;

public class DemonsGateServerOptions
{
    public string PidFileName { get; set; } = "demonsgate.pid";
    public string ConfigFileName { get; set; } = "demonsgate_server.json";
    public LogLevelType LogLevel { get; set; } = LogLevelType.Information;
    public string RootDirectory { get; set; }
}
