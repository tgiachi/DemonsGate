using System.Reflection;
using DemonsGate.Services.Data.Internal.Version;
using DemonsGate.Services.Interfaces;

namespace DemonsGate.Services.Impl;

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
