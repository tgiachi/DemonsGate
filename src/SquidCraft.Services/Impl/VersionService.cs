using System.Reflection;
using SquidCraft.Services.Data.Internal.Version;
using SquidCraft.Services.Interfaces;

namespace SquidCraft.Services.Impl;

/// <summary>
/// Implements the version service for retrieving application version information.
/// </summary>
public class VersionService : IVersionService
{
    public VersionInfoData GetVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0";
        var appName = assembly.GetName().Name ?? "DemonsGate";
        var codeName = "Inferno";

        return new VersionInfoData(appName, codeName, version);
    }
}
