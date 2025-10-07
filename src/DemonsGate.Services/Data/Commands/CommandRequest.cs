using DemonsGate.Services.Types;

namespace DemonsGate.Services.Data.Commands;

/// <summary>
/// public record CommandRequest(string Command, string[] Arguments, CommandSourceType SourceType, int SourceId);.
/// </summary>
public record CommandRequest(string Command, string[] Arguments, CommandSourceType SourceType, int SourceId);
