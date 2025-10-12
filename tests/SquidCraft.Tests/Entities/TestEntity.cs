using SquidCraft.Entities.Attributes;
using SquidCraft.Entities.Models.Base;
using MemoryPack;

namespace SquidCraft.Tests.Entities;

[MemoryPackable]
[Entity("test_entities.dgf")]
public partial class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
