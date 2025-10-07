namespace DemonsGate.Services.Types;

[Flags]
/// <summary>
/// public enum CommandSourceType.
/// </summary>
public enum CommandSourceType
{
    Console,
    InGame,
    All = Console | InGame
}

