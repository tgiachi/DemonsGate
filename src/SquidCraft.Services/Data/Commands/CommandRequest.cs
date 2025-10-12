using SquidCraft.Services.Types;

namespace SquidCraft.Services.Data.Commands;

/// <summary>
/// public record CommandRequest(string Command, string[] Arguments, CommandSourceType SourceType, int SourceId);.
/// </summary>
public record CommandRequest(string Command, string[] Arguments, CommandSourceType SourceType, int SourceId);
