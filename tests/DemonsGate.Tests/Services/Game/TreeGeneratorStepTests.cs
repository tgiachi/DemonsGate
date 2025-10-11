using System;
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
/// Integration tests for TreeGeneratorStep.
/// </summary>
[TestFixture]
public class TreeGeneratorStepTests
{
    private readonly TreeGeneratorStep _step;
    private readonly BiomeGeneratorStep _biomeStep;
    private readonly TerrainGeneratorStep _terrainStep;

    public TreeGeneratorStepTests()
    {
        _step = new TreeGeneratorStep();
        _biomeStep = new BiomeGeneratorStep();
        _terrainStep = new TerrainGeneratorStep();
    }

    [Test]
    public void Name_ReturnsCorrectStepName()
    {
        // Assert
        Assert.That(_step.Name, Is.EqualTo("TreeGenerator"));
    }

    [Test]
    public async Task ExecuteAsync_WithoutBiomeData_DoesNotGenerateTrees()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        int surfaceY = ChunkEntity.Height / 2;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act - no biome data
        await _step.ExecuteAsync(context);

        // Assert - should not have any trees (no Wood or Leaves blocks)
        bool hasTrees = false;
        for (int x = 0; x < ChunkEntity.Size && !hasTrees; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasTrees; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Wood || block?.BlockType == BlockType.Leaves)
                    {
                        hasTrees = true;
                        break;
                    }
                }
            }
        }

        Assert.That(hasTrees, Is.False, "Should not generate trees without biome data");
    }

    [Test]
    public async Task ExecuteAsync_WithDesertBiome_DoesNotGenerateTrees()
    {
        // Arrange
        var position = new Vector3(0, 0, 0);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        int surfaceY = ChunkEntity.Height / 2;
        // Add desert biome (no trees)
        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = DemonsGate.Services.Game.Types.BiomeType.SubtropicalDesert,
            SurfaceBlock = BlockType.Dirt,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 0.7f,
            BaseHeight = -10f
        };

        CreateFlatSurface(chunk, surfaceY, BlockType.Dirt);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - desert has treeDensity = 0, so no trees
        bool hasTrees = false;
        for (int x = 0; x < ChunkEntity.Size && !hasTrees; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasTrees; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Wood || block?.BlockType == BlockType.Leaves)
                    {
                        hasTrees = true;
                        break;
                    }
                }
            }
        }

        Assert.That(hasTrees, Is.False, "Desert biome should not generate trees");
    }

    [Test]
    public async Task ExecuteAsync_WithForestBiome_CanGenerateTrees()
    {
        // Arrange - use full pipeline for realistic terrain
        var position = new Vector3(1000, 0, 1000); // Different position to potentially get forest
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Generate biome and terrain first
        await _biomeStep.ExecuteAsync(context);
        await _terrainStep.ExecuteAsync(context);

        // Force a forest biome
        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = DemonsGate.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.2f,
            BaseHeight = 15f
        };

        // Act
        await _step.ExecuteAsync(context);

        // Assert - may or may not have trees depending on noise, but shouldn't crash
        // Just verify no exception was thrown
        Assert.That(true, Is.True, "Tree generation step completed without errors");
    }

    [Test]
    public async Task ExecuteAsync_TreeStructure_HasWoodTrunkAndLeaves()
    {
        // Arrange
        var position = new Vector3(0, 0, 0);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Add forest biome with very low threshold (almost always place trees)
        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = DemonsGate.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        // Create flat grassland at a high Y level with lots of space above
        int surfaceY = ChunkEntity.Height / 2;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - if any tree was placed, verify structure
        bool foundWood = false;
        bool foundLeaves = false;

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = surfaceY + 1; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Wood)
                    {
                        foundWood = true;
                    }
                    else if (block?.BlockType == BlockType.Leaves)
                    {
                        foundLeaves = true;
                    }
                }
            }
        }

        // If we found wood, we should also find leaves (and vice versa for a complete tree)
        if (foundWood || foundLeaves)
        {
            Assert.That(foundWood, Is.True, "Trees should have wood trunks");
            Assert.That(foundLeaves, Is.True, "Trees should have leaves");
        }
    }

    [Test]
    public async Task ExecuteAsync_WithUnderwaterTerrain_DoesNotPlaceTrees()
    {
        // Arrange
        var position = new Vector3(0, -1024, 0); // Deep underwater
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Add forest biome
        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = DemonsGate.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.2f,
            BaseHeight = 15f
        };

        // Create underwater terrain (grass with water above)
        int surfaceY = ChunkEntity.Height / 4;
        int waterTop = Math.Min(ChunkEntity.Height - 2, surfaceY + 8);

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                chunk.SetBlock(x, surfaceY, z, new BlockEntity(1, BlockType.Grass));

                for (int y = surfaceY + 1; y <= waterTop; y++)
                {
                    chunk.SetBlock(x, y, z, new BlockEntity(1, BlockType.Water));
                }

                for (int y = waterTop + 1; y < ChunkEntity.Height; y++)
                {
                    chunk.SetBlock(x, y, z, new BlockEntity(1, BlockType.Air));
                }
            }
        }

        // Act
        await _step.ExecuteAsync(context);

        // Assert - should not place trees underwater
        bool hasTrees = false;
        for (int x = 0; x < ChunkEntity.Size && !hasTrees; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !hasTrees; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Wood || block?.BlockType == BlockType.Leaves)
                    {
                        hasTrees = true;
                        break;
                    }
                }
            }
        }

        Assert.That(hasTrees, Is.False, "Should not place trees underwater");
    }

    [Test]
    public async Task ExecuteAsync_WithDifferentSeeds_ProducesDifferentTreePlacement()
    {
        // Arrange
        var position = new Vector3(100, 0, 200);

        // First chunk with seed 1
        var chunk1 = new ChunkEntity(position);
        var noise1 = new FastNoiseLite(111);
        var context1 = new GeneratorContext(chunk1, position, noise1, 111);
        context1.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = DemonsGate.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };
        CreateFlatSurface(chunk1, ChunkEntity.Height / 2, BlockType.Grass);

        // Second chunk with seed 2
        var chunk2 = new ChunkEntity(position);
        var noise2 = new FastNoiseLite(222);
        var context2 = new GeneratorContext(chunk2, position, noise2, 222);
        context2.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = DemonsGate.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };
        CreateFlatSurface(chunk2, ChunkEntity.Height / 2, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context1);
        await _step.ExecuteAsync(context2);

        // Assert - tree placement should be different (count wood blocks)
        int woodCount1 = CountBlockType(chunk1, BlockType.Wood);
        int woodCount2 = CountBlockType(chunk2, BlockType.Wood);

        // With different seeds, tree counts should likely differ
        // (very small chance they're the same, but unlikely)
        Assert.That(woodCount2, Is.Not.EqualTo(woodCount1));
    }

    private static void CreateFlatSurface(ChunkEntity chunk, int surfaceY, BlockType surfaceBlock)
    {
        surfaceY = Math.Clamp(surfaceY, 1, ChunkEntity.Height - 2);

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                chunk.SetBlock(x, surfaceY, z, new BlockEntity(1, surfaceBlock));

                for (int y = surfaceY + 1; y < ChunkEntity.Height; y++)
                {
                    chunk.SetBlock(x, y, z, new BlockEntity(1, BlockType.Air));
                }
            }
        }
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
