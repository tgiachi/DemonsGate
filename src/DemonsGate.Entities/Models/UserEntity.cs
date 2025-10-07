using DemonsGate.Core.Enums;
using DemonsGate.Entities.Attributes;
using DemonsGate.Entities.Models.Base;
using MemoryPack;

namespace DemonsGate.Entities.Models;

[MemoryPackable]
[Entity("users.dgf")]
public partial class UserEntity : BaseEntity
{

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public UserLevelType UserLevel { get; set; } = UserLevelType.User;

    public bool IsLocked { get; set; }

}
