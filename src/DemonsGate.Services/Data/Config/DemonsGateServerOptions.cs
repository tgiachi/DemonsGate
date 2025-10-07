using DemonsGate.Services.Types;

namespace DemonsGate.Services.Data.Config;

public class DemonsGateServerOptions
{
    public string PidFileName { get; set; } = "demonsgate.pid";
    public LogLevelType LogLevel { get; set; } = LogLevelType.Information;
    public string RootDirectory { get; set; }
}
