using SquidCraft.Core.Enums;
using SquidCraft.Entities.Attributes;
using SquidCraft.Entities.Models.Base;
using MemoryPack;

namespace SquidCraft.Entities.Models;

[MemoryPackable]
[Entity("users.dgf")]
public partial class UserEntity : BaseEntity
{

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public UserLevelType UserLevel { get; set; } = UserLevelType.User;

    public bool IsLocked { get; set; }

}
