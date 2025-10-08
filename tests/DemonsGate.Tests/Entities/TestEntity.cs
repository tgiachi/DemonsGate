using DemonsGate.Entities.Attributes;
using DemonsGate.Entities.Models.Base;
using MemoryPack;

namespace DemonsGate.Tests.Entities;

[MemoryPackable]
[Entity("test_entities.dgf")]
public partial class TestEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
