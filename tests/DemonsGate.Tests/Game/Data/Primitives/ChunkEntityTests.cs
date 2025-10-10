using System;
using System.Numerics;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Services.Game.Types;

namespace DemonsGate.Tests.Game.Data.Primitives;

[TestFixture]
public class ChunkEntityTests
{
    private const int TotalBlocks = ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height;

    [Test]
    public void Constructor_ShouldInitializeBlocksWithExpectedLength()
    {
        var chunk = new ChunkEntity(Vector3.Zero);

        Assert.That(chunk.Blocks, Has.Length.EqualTo(TotalBlocks));
        Assert.That(chunk.Position, Is.EqualTo(Vector3.Zero));
    }

    [Test]
    public void SetBlock_WithValidCoordinates_ShouldStoreBlock()
    {
        var chunk = new ChunkEntity(Vector3.Zero);
        var block = new BlockEntity(42, BlockType.Dirt);

        chunk.SetBlock(1, 2, 3, block);
        var retrieved = chunk.GetBlock(1, 2, 3);

        Assert.That(retrieved, Is.SameAs(block));
    }

    [Test]
    public void SetBlock_WithNullBlock_ShouldThrow()
    {
        var chunk = new ChunkEntity(Vector3.Zero);

        Assert.Throws<ArgumentNullException>(() => chunk.SetBlock(1, 1, 1, null!));
    }

    [Test]
    public void GetBlock_ByIndex_ShouldRespectBoundsChecks()
    {
        var chunk = new ChunkEntity(Vector3.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => chunk.GetBlock(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => chunk.GetBlock(TotalBlocks));
    }

    [Test]
    public void GetBlock_WithOutOfRangeCoordinates_ShouldThrow()
    {
        var chunk = new ChunkEntity(Vector3.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => chunk.GetBlock(ChunkEntity.Size, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => chunk.GetBlock(0, ChunkEntity.Height, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => chunk.GetBlock(0, 0, ChunkEntity.Size));
    }

    [Test]
    public void GetIndex_ShouldMatchManualCalculation()
    {
        var chunk = new ChunkEntity(Vector3.Zero);
        const int x = 3;
        const int y = 5;
        const int z = 7;

        var expected = x + y * ChunkEntity.Size + z * ChunkEntity.Size * ChunkEntity.Height;

        var index = ChunkEntity.GetIndex(x, y, z);

        Assert.That(index, Is.EqualTo(expected));
    }

    [Test]
    public void SetBlock_ByIndex_ShouldStoreBlock()
    {
        var chunk = new ChunkEntity(Vector3.Zero);
        var block = new BlockEntity(99, BlockType.Grass);
        var index = ChunkEntity.GetIndex(2, 3, 4);

        chunk.SetBlock(index, block);
        var retrieved = chunk.GetBlock(index);

        Assert.That(retrieved, Is.SameAs(block));
    }
}
