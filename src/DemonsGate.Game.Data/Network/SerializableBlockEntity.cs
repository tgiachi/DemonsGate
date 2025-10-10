using DemonsGate.Game.Data.Primitives;
using DemonsGate.Services.Game.Types;
using MemoryPack;

namespace DemonsGate.Game.Data.Network;

[MemoryPackable]
public partial class SerializableBlockEntity
{
    public long Id { get; set; }

    public BlockType BlockType { get; set; }


    public static implicit operator SerializableBlockEntity(BlockEntity blockEntity)
    {
        return new SerializableBlockEntity()
        {
            BlockType = blockEntity.BlockType,
            Id = blockEntity.Id,
        };
    }

    public static implicit operator BlockEntity(SerializableBlockEntity blockEntity)
    {
        return new BlockEntity(blockEntity.Id, blockEntity.BlockType);
    }
}
