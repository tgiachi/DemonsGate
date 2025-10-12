using System.Numerics;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;
using SquidCraft.Services.Game.Generation.Noise;
using SquidCraft.Services.Game.Impl.Pipeline;
using SquidCraft.Services.Game.Impl.Pipeline.Steps;
using NUnit.Framework;

namespace SquidCraft.Tests.Services.Game;

/// <summary>
/// Integration tests for CaveGeneratorStep.
/// </summary>
[TestFixture]
public class CaveGeneratorStepTests
{
    private readonly CaveGeneratorStep _step;
    private readonly TerrainGeneratorStep _terrainStep;

    public CaveGeneratorStepTests()
    {
        _step = new CaveGeneratorStep();
        _terrainStep = new TerrainGeneratorStep();
    }

    [Test]
    public void Name_ReturnsCorrectStepName()
    {
        // Assert
        Assert.That(_step.Name, Is.EqualTo("CaveGenerator"));
    }

    [Test]
    public async Task ExecuteAsync_WithSolidTerrain_CarvesCaves()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Generate terrain first
        await _terrainStep.ExecuteAsync(context);

        // Count solid blocks before cave generation
        int solidBlocksBefore = CountSolidBlocks(chunk);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - should have fewer solid blocks after carving caves
        int solidBlocksAfter = CountSolidBlocks(chunk);
        Assert.That(solidBlocksAfter, Is.LessThan(solidBlocksBefore),
            "Cave generation should remove some solid blocks");
    }

    [Test]
    public async Task ExecuteAsync_DoesNotRemoveBedrock()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Generate terrain first
        await _terrainStep.ExecuteAsync(context);

        // Count bedrock blocks before
        int bedrockBefore = CountBlockType(chunk, BlockType.Bedrock);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - bedrock count should remain the same
        int bedrockAfter = CountBlockType(chunk, BlockType.Bedrock);
        Assert.That(bedrockAfter, Is.EqualTo(bedrockBefore));
    }

    [Test]
    public async Task ExecuteAsync_WithDifferentSeeds_ProducesDifferentCaves()
    {
        // Arrange
        var position = new Vector3(100, 0, 200);

        // First chunk with seed 1
        var chunk1 = new ChunkEntity(position);
        var noise1 = new FastNoiseLite(111);
        var context1 = new GeneratorContext(chunk1, position, noise1, 111);
        await _terrainStep.ExecuteAsync(context1);
        await _step.ExecuteAsync(context1);

        // Second chunk with seed 2
        var chunk2 = new ChunkEntity(position);
        var noise2 = new FastNoiseLite(222);
        var context2 = new GeneratorContext(chunk2, position, noise2, 222);
        await _terrainStep.ExecuteAsync(context2);
        await _step.ExecuteAsync(context2);

        // Assert - different seeds should produce different cave patterns
        int airBlocks1 = CountBlockType(chunk1, BlockType.Air);
        int airBlocks2 = CountBlockType(chunk2, BlockType.Air);

        Assert.That(airBlocks2, Is.Not.EqualTo(airBlocks1));
    }

    [Test]
    public async Task ExecuteAsync_WithSameSeed_ProducesDeterministicCaves()
    {
        // Arrange
        var position = new Vector3(100, 0, 200);
        var seed = 54321;

        // First chunk
        var chunk1 = new ChunkEntity(position);
        var noise1 = new FastNoiseLite(seed);
        var context1 = new GeneratorContext(chunk1, position, noise1, seed);
        await _terrainStep.ExecuteAsync(context1);
        await _step.ExecuteAsync(context1);

        // Second chunk with same seed
        var chunk2 = new ChunkEntity(position);
        var noise2 = new FastNoiseLite(seed);
        var context2 = new GeneratorContext(chunk2, position, noise2, seed);
        await _terrainStep.ExecuteAsync(context2);
        await _step.ExecuteAsync(context2);

        // Assert - chunks should be identical
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block1 = chunk1.GetBlock(x, y, z);
                    var block2 = chunk2.GetBlock(x, y, z);
                    Assert.That(block2?.BlockType, Is.EqualTo(block1?.BlockType));
                }
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_CreatesAirPockets()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Generate terrain first
        await _terrainStep.ExecuteAsync(context);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - should have air pockets underground (not just at surface)
        bool hasUndergroundAir = false;
        for (int x = 0; x < ChunkEntity.Size && !hasUndergroundAir; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasUndergroundAir; z++)
            {
                // Check underground area (Y: 10-50)
                for (int y = 10; y < 50; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Air)
                    {
                        // Verify this is truly underground by checking blocks around it
                        var blockBelow = chunk.GetBlock(x, Math.Max(0, y - 1), z);
                        if (blockBelow?.BlockType != BlockType.Air)
                        {
                            hasUndergroundAir = true;
                            break;
                        }
                    }
                }
            }
        }

        Assert.That(hasUndergroundAir, Is.True, "Should create underground air pockets (caves)");
    }

    [Test]
    public async Task ExecuteAsync_WithEmptyChunk_DoesNotCrash()
    {
        // Arrange - chunk with all air
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act & Assert - should not crash
        await _step.ExecuteAsync(context);
        Assert.That(true, Is.True, "Should handle empty chunk without crashing");
    }

    [Test]
    public async Task ExecuteAsync_PreservesWaterBlocks()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Generate terrain first (which may include water)
        await _terrainStep.ExecuteAsync(context);

        // Count water blocks before
        int waterBefore = CountBlockType(chunk, BlockType.Water);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - water count should be the same (caves don't remove water)
        int waterAfter = CountBlockType(chunk, BlockType.Water);
        Assert.That(waterAfter, Is.EqualTo(waterBefore));
    }

    private static int CountSolidBlocks(ChunkEntity chunk)
    {
        int count = 0;
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block != null &&
                        block.BlockType != BlockType.Air &&
                        block.BlockType != BlockType.Water)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }

    private static int CountBlockType(ChunkEntity chunk, BlockType blockType)
    {
        int count = 0;
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == blockType)
                    {
                        count++;
                    }
                }
            }
        }
        return count;
    }
}
