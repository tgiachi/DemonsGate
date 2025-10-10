using DemonsGate.Services.Game.Types;

namespace DemonsGate.Game.Data.Primitives;

public class BlockEntity
{
    public long Id { get; set; }
    public BlockType BlockType { get; set; }


    public BlockEntity(long id, BlockType blockType)
    {
        Id = id;
        BlockType = blockType;
    }

    public override string ToString() => $"BlockEntity({Id}, {BlockType})";
}
