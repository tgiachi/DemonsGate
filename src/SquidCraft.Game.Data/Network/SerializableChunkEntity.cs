using System;
using System.Numerics;
using SquidCraft.Game.Data.Primitives;
using MemoryPack;

namespace SquidCraft.Game.Data.Network;

/// <summary>
/// Serializable transport model for <see cref="ChunkEntity"/> instances.
/// </summary>
[MemoryPackable]
public partial class SerializableChunkEntity
{
    /// <summary>
    /// Gets or sets the chunk position in world space.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the serialized block buffer backing the chunk.
    /// </summary>
    public SerializableBlockEntity?[] Blocks { get; set; } = [];

    /// <summary>
    /// Creates a serializable chunk from a runtime chunk entity.
    /// </summary>
    /// <param name="chunkEntity">Runtime chunk entity to convert.</param>
    /// <returns>Serializable chunk entity.</returns>
    public static implicit operator SerializableChunkEntity(ChunkEntity chunkEntity)
    {
        ArgumentNullException.ThrowIfNull(chunkEntity);

        var serializable = new SerializableChunkEntity
        {
            Position = chunkEntity.Position,
            Blocks = new SerializableBlockEntity?[chunkEntity.Blocks.Length],
        };

        for (var i = 0; i < chunkEntity.Blocks.Length; i++)
        {
            var block = chunkEntity.Blocks[i];
            serializable.Blocks[i] = block is null ? null : (SerializableBlockEntity)block;
        }

        return serializable;
    }

    /// <summary>
    /// Rehydrates a runtime chunk entity from its serializable representation.
    /// </summary>
    /// <param name="serializableChunk">Serializable chunk entity to convert.</param>
    /// <returns>Runtime chunk entity.</returns>
    public static implicit operator ChunkEntity(SerializableChunkEntity serializableChunk)
    {
        ArgumentNullException.ThrowIfNull(serializableChunk);

        var chunkEntity = new ChunkEntity(serializableChunk.Position);
        var blocks = serializableChunk.Blocks;

        if (blocks is null || blocks.Length == 0)
        {
            return chunkEntity;
        }

        var length = Math.Min(blocks.Length, chunkEntity.Blocks.Length);

        for (var i = 0; i < length; i++)
        {
            var block = blocks[i];
            if (block is null)
            {
                continue;
            }

            chunkEntity.SetBlock(i, (BlockEntity)block);
        }

        return chunkEntity;
    }
}
