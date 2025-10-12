using MemoryPack;

namespace SquidCraft.Entities.Models.Base;

[MemoryPackable]
public partial class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime Created { get; set; }

    public DateTime Updated { get; set; }
}
