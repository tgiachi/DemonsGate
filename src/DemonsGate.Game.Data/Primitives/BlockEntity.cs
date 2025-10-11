using DemonsGate.Game.Data.Types;

namespace DemonsGate.Game.Data.Primitives;

/// <summary>
/// Represents a single block instance within a chunk, including its identifier and type.
/// </summary>
public class BlockEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the block instance.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the semantic type for the block.
    /// </summary>
    public BlockType BlockType { get; set; }


    /// <summary>
    /// Initializes a new <see cref="BlockEntity"/> with the provided identifier and type.
    /// </summary>
    /// <param name="id">Unique identifier assigned to the block.</param>
    /// <param name="blockType">The type of block represented by this entity.</param>
    public BlockEntity(long id, BlockType blockType)
    {
        Id = id;
        BlockType = blockType;
    }

    public override string ToString() => $"BlockEntity({Id}, {BlockType})";
}
