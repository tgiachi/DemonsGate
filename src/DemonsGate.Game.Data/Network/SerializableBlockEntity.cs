using DemonsGate.Game.Data.Primitives;
using DemonsGate.Game.Data.Types;
using MemoryPack;

namespace DemonsGate.Game.Data.Network;

/// <summary>
/// Serializable representation of a <see cref="BlockEntity"/> for network transport.
/// </summary>
[MemoryPackable]
public partial class SerializableBlockEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the block instance.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the semantic block type.
    /// </summary>
    public BlockType BlockType { get; set; }


    /// <summary>
    /// Creates a serializable block representation from a runtime entity.
    /// </summary>
    /// <param name="blockEntity">Runtime block entity to convert.</param>
    /// <returns>Serializable block entity.</returns>
    public static implicit operator SerializableBlockEntity(BlockEntity blockEntity)
    {
        return new SerializableBlockEntity()
        {
            BlockType = blockEntity.BlockType,
            Id = blockEntity.Id,
        };
    }

    /// <summary>
    /// Rehydrates a runtime block entity from its serializable counterpart.
    /// </summary>
    /// <param name="blockEntity">Serializable block entity to convert.</param>
    /// <returns>Runtime block entity.</returns>
    public static implicit operator BlockEntity(SerializableBlockEntity blockEntity)
    {
        return new BlockEntity(blockEntity.Id, blockEntity.BlockType);
    }
}
