using DemonsGate.Core.Interfaces.Services;
using DemonsGate.Services.Data.Internal.Version;

namespace DemonsGate.Services.Interfaces;

/// <summary>
///     Interface for the version service that provides version information.
/// </summary>
public interface IVersionService : IDemonsGateService
{
    VersionInfoData GetVersionInfo();
}
