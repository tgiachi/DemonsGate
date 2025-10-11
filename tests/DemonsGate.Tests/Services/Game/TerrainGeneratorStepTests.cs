using System.Numerics;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Game.Data.Types;
using DemonsGate.Services.Game.Data;
using DemonsGate.Services.Game.Generation.Noise;
using DemonsGate.Services.Game.Impl.Pipeline;
using DemonsGate.Services.Game.Impl.Pipeline.Steps;
using NUnit.Framework;

namespace DemonsGate.Tests.Services.Game;

/// <summary>
/// Integration tests for TerrainGeneratorStep.
/// </summary>
[TestFixture]
public class TerrainGeneratorStepTests
{
    private readonly TerrainGeneratorStep _step;

    public TerrainGeneratorStepTests()
    {
        _step = new TerrainGeneratorStep();
    }

    [Test]
    public void Name_ReturnsCorrectStepName()
    {
        // Assert
        Assert.That(_step.Name, Is.EqualTo("TerrainGenerator"));
    }

    [Test]
    public async Task ExecuteAsync_GeneratesTerrain()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - chunk should have non-air blocks
        bool hasNonAirBlocks = false;
        for (int x = 0; x < ChunkEntity.Size && !hasNonAirBlocks; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasNonAirBlocks; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType != BlockType.Air)
                    {
                        hasNonAirBlocks = true;
                        break;
                    }
                }
            }
        }

        Assert.That(hasNonAirBlocks, Is.True, "Terrain should generate non-air blocks");
    }

    [Test]
    public async Task ExecuteAsync_GeneratesBedrockAtBottom()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - bottom layer should be bedrock
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                var block = chunk.GetBlock(x, 0, z);
                Assert.That(block, Is.Not.Null);
                Assert.That(block.BlockType, Is.EqualTo(BlockType.Bedrock));
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_GeneratesStoneUnderground()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - should have stone somewhere underground
        bool hasStone = false;
        for (int x = 0; x < ChunkEntity.Size && !hasStone; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasStone; z++)
            {
                for (int y = 1; y < 50; y++) // Check underground area
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Stone)
                    {
                        hasStone = true;
                        break;
                    }
                }
            }
        }

        Assert.That(hasStone, Is.True, "Should generate stone underground");
    }

    [Test]
    public async Task ExecuteAsync_WithBiomeData_UsesBiomeSurfaceBlocks()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Add biome data for a snow biome
        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = DemonsGate.Services.Game.Types.BiomeType.Snow,
            SurfaceBlock = BlockType.Snow,
            SubsurfaceBlock = BlockType.Snow,
            HeightMultiplier = 1.3f,
            BaseHeight = 30f,
            Temperature = 0.1f,
            Moisture = 0.5f,
            Elevation = 0.9f
        };

        // Act
        await _step.ExecuteAsync(context);

        // Assert - should have snow blocks at surface (above sea level)
        bool hasSnow = false;
        for (int x = 0; x < ChunkEntity.Size && !hasSnow; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasSnow; z++)
            {
                // Find surface
                for (int y = ChunkEntity.Height - 1; y >= 0; y--)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block == null)
                    {
                        continue;
                    }

                    if (block.BlockType != BlockType.Air)
                    {
                        if (block.BlockType == BlockType.Snow)
                        {
                            hasSnow = true;
                        }
                        break;
                    }
                }
            }
        }

        Assert.That(hasSnow, Is.True, "Should use biome surface block (Snow) for terrain above sea level");
    }

    [Test]
    public async Task ExecuteAsync_WithoutBiomeData_UsesDefaultBlocks()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act - no biome data
        await _step.ExecuteAsync(context);

        // Assert - should use default grass/dirt
        bool hasDefaultBlocks = false;
        for (int x = 0; x < ChunkEntity.Size && !hasDefaultBlocks; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasDefaultBlocks; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Grass || block?.BlockType == BlockType.Dirt)
                    {
                        hasDefaultBlocks = true;
                        break;
                    }
                }
            }
        }

        Assert.That(hasDefaultBlocks, Is.True, "Should use default blocks (Grass/Dirt) when no biome data");
    }

    [Test]
    public async Task ExecuteAsync_GeneratesWaterBelowSeaLevel()
    {
        // Arrange - position at Y=-1024 to ensure we're well below sea level
        var position = new Vector3(0, -1024, 0);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - should have water blocks
        bool hasWater = false;
        for (int x = 0; x < ChunkEntity.Size && !hasWater; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasWater; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Water)
                    {
                        hasWater = true;
                        break;
                    }
                }
            }
        }

        Assert.That(hasWater, Is.True, "Should generate water below sea level");
    }

    [Test]
    public async Task ExecuteAsync_WithSameSeed_ProducesDeterministicTerrain()
    {
        // Arrange
        var position = new Vector3(100, 0, 200);
        var seed = 54321;

        var chunk1 = new ChunkEntity(position);
        var noise1 = new FastNoiseLite(seed);
        var context1 = new GeneratorContext(chunk1, position, noise1, seed);

        var chunk2 = new ChunkEntity(position);
        var noise2 = new FastNoiseLite(seed);
        var context2 = new GeneratorContext(chunk2, position, noise2, seed);

        // Act
        await _step.ExecuteAsync(context1);
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
    public async Task ExecuteAsync_EnsuresBedrockAtWorldYNegative1024()
    {
        // Arrange - position at exact bedrock world Y
        var position = new Vector3(0, -1024, 0);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - Y=0 in this chunk is world Y=-1024, should be bedrock
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                var block = chunk.GetBlock(x, 0, z);
                    Assert.That(block, Is.Not.Null);
                Assert.That(block.BlockType, Is.EqualTo(BlockType.Bedrock));
            }
        }
    }
}
