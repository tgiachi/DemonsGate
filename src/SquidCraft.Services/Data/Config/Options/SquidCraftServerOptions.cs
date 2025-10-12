using SquidCraft.Services.Types;

namespace SquidCraft.Services.Data.Config.Options;

/// <summary>
/// public class SquidCraftServerOptions.
/// </summary>
public class SquidCraftServerOptions
{
    public string PidFileName { get; set; } = "squidcraft.pid";
    public string ConfigFileName { get; set; } = "squidcraft_server.json";
    public LogLevelType LogLevel { get; set; } = LogLevelType.Information;
    public string RootDirectory { get; set; }
    public bool IsShellEnabled { get; set; } = true;
}
