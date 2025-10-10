using System;
using System.Numerics;
using DemonsGate.Game.Data.Primitives;
using MemoryPack;

namespace DemonsGate.Game.Data.Network;

[MemoryPackable]
public partial class SerializableChunkEntity
{
    public Vector3 Position { get; set; }

    public SerializableBlockEntity?[] Blocks { get; set; } = [];

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
