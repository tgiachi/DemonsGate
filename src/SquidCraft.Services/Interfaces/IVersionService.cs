using SquidCraft.Core.Interfaces.Services;
using SquidCraft.Services.Data.Internal.Version;

namespace SquidCraft.Services.Interfaces;

/// <summary>
///     Interface for the version service that provides version information.
/// </summary>
public interface IVersionService : ISquidCraftService
{
    VersionInfoData GetVersionInfo();
}
