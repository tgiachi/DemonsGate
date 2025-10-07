namespace DemonsGate.Services.Types;

[Flags]
/// <summary>
/// public enum UserLevelType.
/// </summary>
public enum UserLevelType
{
    User = 1,
    Moderator = 2,
    Admin = 4,
    SuperAdmin = 8,
    All = User | Moderator | Admin | SuperAdmin

}
