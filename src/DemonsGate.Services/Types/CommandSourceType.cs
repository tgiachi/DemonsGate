namespace DemonsGate.Services.Types;

[Flags]
/// <summary>
/// public enum CommandSourceType.
/// </summary>
public enum CommandSourceType
{
    InGame,
    Console,
    All = Console | InGame
}

