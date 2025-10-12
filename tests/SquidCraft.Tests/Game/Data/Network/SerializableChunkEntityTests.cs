using System;
using System.Collections.Generic;
using System.Numerics;
using SquidCraft.Game.Data.Network;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Tests.Game.Data.Network;

[TestFixture]
public class SerializableChunkEntityTests
{
    private const int TotalBlocks = ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height;

    [Test]
    public void ImplicitConversion_FromChunkEntity_ShouldCopyPositionAndBlocks()
    {
        var chunk = new ChunkEntity(new Vector3(10, 20, 30));
        var block = new BlockEntity(7, BlockType.Dirt);
        var index = ChunkEntity.GetIndex(1, 1, 1);
        chunk.SetBlock(index, block);

        SerializableChunkEntity serializable = chunk;

        Assert.That(serializable.Position, Is.EqualTo(chunk.Position));
        Assert.That(serializable.Blocks, Has.Length.EqualTo(TotalBlocks));
        Assert.That(serializable.Blocks[index], Is.Not.Null);
        Assert.That(serializable.Blocks[index]!.Id, Is.EqualTo(block.Id));
        Assert.That(serializable.Blocks[index]!.BlockType, Is.EqualTo(block.BlockType));
    }

    [Test]
    public void ImplicitConversion_ToChunkEntity_ShouldRestoreBlocks()
    {
        var serializable = new SerializableChunkEntity
        {
            Position = new Vector3(3, 4, 5),
            Blocks = new SerializableBlockEntity?[TotalBlocks],
        };

        var expectedBlock = new SerializableBlockEntity
        {
            Id = 42,
            BlockType = BlockType.Grass,
        };

        const int index = 5;
        serializable.Blocks![index] = expectedBlock;

        ChunkEntity chunk = serializable;

        Assert.That(chunk.Position, Is.EqualTo(serializable.Position));
        var restoredBlock = chunk.GetBlock(index);
        Assert.That(restoredBlock, Is.Not.Null);
        Assert.That(restoredBlock!.Id, Is.EqualTo(expectedBlock.Id));
        Assert.That(restoredBlock.BlockType, Is.EqualTo(expectedBlock.BlockType));
    }

    [Test]
    public void ImplicitConversion_ToChunkEntity_WithNullOrShortBlocks_ShouldHandleGracefully()
    {
        var serializable = new SerializableChunkEntity
        {
            Position = Vector3.Zero,
            Blocks = new SerializableBlockEntity?[2],
        };

        serializable.Blocks![1] = new SerializableBlockEntity
        {
            Id = 99,
            BlockType = BlockType.Dirt,
        };

        ChunkEntity chunk = serializable;

        Assert.That(chunk.Blocks, Has.Length.EqualTo(TotalBlocks));
        Assert.That(chunk.GetBlock(0), Is.Null);
        Assert.That(chunk.GetBlock(1), Is.Not.Null);
        Assert.That(chunk.GetBlock(1)!.Id, Is.EqualTo(99));

        serializable.Blocks = null!;

        chunk = serializable;

        Assert.That(chunk.Blocks, Has.Length.EqualTo(TotalBlocks));
    }


}
