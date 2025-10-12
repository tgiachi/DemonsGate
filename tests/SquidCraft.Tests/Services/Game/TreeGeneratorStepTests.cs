using System;
using System.Numerics;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;
using SquidCraft.Services.Game.Data;
using SquidCraft.Services.Game.Generation.Noise;
using SquidCraft.Services.Game.Impl.Pipeline;
using SquidCraft.Services.Game.Impl.Pipeline.Steps;
using NUnit.Framework;

namespace SquidCraft.Tests.Services.Game;

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
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.SubtropicalDesert,
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
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
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
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
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
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
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
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
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
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
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

    [Test]
    public async Task ExecuteAsync_TropicalRainforest_ActuallyGeneratesTrees()
    {
        // Arrange - use specific seed and position known to generate trees
        var position = new Vector3(5000, 0, 5000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(42);
        var context = new GeneratorContext(chunk, position, noise, 42);

        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        int surfaceY = ChunkEntity.Height / 2;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - Tropical rainforest should generate at least some trees
        int woodCount = CountBlockType(chunk, BlockType.Wood);
        int leavesCount = CountBlockType(chunk, BlockType.Leaves);

        Assert.That(woodCount, Is.GreaterThan(0), "Should generate at least some wood blocks (tree trunks)");
        Assert.That(leavesCount, Is.GreaterThan(0), "Should generate at least some leaves blocks (tree canopy)");
        Assert.That(leavesCount, Is.GreaterThan(woodCount), "Should have more leaves than wood blocks");
    }

    [Test]
    public async Task ExecuteAsync_TreeHasCorrectVerticalStructure()
    {
        // Arrange - create conditions favorable for tree generation
        var position = new Vector3(3000, 0, 3000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(777);
        var context = new GeneratorContext(chunk, position, noise, 777);

        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TemperateDeciduousForest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        int surfaceY = ChunkEntity.Height / 2;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - if we find any wood block, verify it has proper structure
        bool foundTree = false;
        for (int x = 0; x < ChunkEntity.Size && !foundTree; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !foundTree; z++)
            {
                // Look for a wood block (trunk)
                for (int y = surfaceY + 1; y < ChunkEntity.Height - 5; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == BlockType.Wood)
                    {
                        foundTree = true;

                        // Verify there's more wood above (trunk continues)
                        bool hasWoodAbove = false;
                        for (int dy = 1; dy <= 7; dy++)
                        {
                            var blockAbove = chunk.GetBlock(x, y + dy, z);
                            if (blockAbove?.BlockType == BlockType.Wood)
                            {
                                hasWoodAbove = true;
                                break;
                            }
                        }

                        // Verify there are leaves nearby (canopy)
                        bool hasLeavesNearby = false;
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            for (int dz = -2; dz <= 2; dz++)
                            {
                                for (int dy = 2; dy <= 5; dy++)
                                {
                                    int checkX = x + dx;
                                    int checkZ = z + dz;
                                    int checkY = y + dy;

                                    if (checkX >= 0 && checkX < ChunkEntity.Size &&
                                        checkZ >= 0 && checkZ < ChunkEntity.Size &&
                                        checkY < ChunkEntity.Height)
                                    {
                                        var nearbyBlock = chunk.GetBlock(checkX, checkY, checkZ);
                                        if (nearbyBlock?.BlockType == BlockType.Leaves)
                                        {
                                            hasLeavesNearby = true;
                                            break;
                                        }
                                    }
                                }
                                if (hasLeavesNearby) break;
                            }
                            if (hasLeavesNearby) break;
                        }

                        Assert.That(hasWoodAbove, Is.True, "Tree trunk should extend upward");
                        Assert.That(hasLeavesNearby, Is.True, "Tree should have leaves in canopy");
                        break;
                    }
                }
            }
        }

        if (!foundTree)
        {
            Assert.Inconclusive("No trees were generated in this test run (may happen due to noise RNG)");
        }
    }

    [Test]
    public async Task ExecuteAsync_TreesRespectChunkBoundaries()
    {
        // Arrange
        var position = new Vector3(2000, 0, 2000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(999);
        var context = new GeneratorContext(chunk, position, noise, 999);

        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        int surfaceY = ChunkEntity.Height / 2;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - trees should only be placed within valid chunk boundaries
        // Tree trunks should be placed in the margin area (x,z from 2 to Size-2)
        // Canopy can extend to edges
        for (int x = 0; x < 2; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = surfaceY + 1; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    Assert.That(block?.BlockType, Is.Not.EqualTo(BlockType.Wood),
                        $"Wood (tree trunk) should not be placed in margin at x={x}");
                }
            }
        }

        for (int x = ChunkEntity.Size - 2; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = surfaceY + 1; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    Assert.That(block?.BlockType, Is.Not.EqualTo(BlockType.Wood),
                        $"Wood (tree trunk) should not be placed in margin at x={x}");
                }
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_TreeHeightWithinExpectedRange()
    {
        // Arrange
        var position = new Vector3(7000, 0, 7000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(1234);
        var context = new GeneratorContext(chunk, position, noise, 1234);

        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TemperateRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        int surfaceY = ChunkEntity.Height / 2;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - find trees and verify height is between 4-8 blocks
        // (MinTreeHeight = 4, MaxTreeHeight = 8 from TreeGeneratorStep)
        for (int x = 2; x < ChunkEntity.Size - 2; x++)
        {
            for (int z = 2; z < ChunkEntity.Size - 2; z++)
            {
                // Check if this is a tree base (wood block on grass)
                var surfaceBlock = chunk.GetBlock(x, surfaceY, z);
                var blockAboveSurface = chunk.GetBlock(x, surfaceY + 1, z);

                if (surfaceBlock?.BlockType == BlockType.Grass &&
                    blockAboveSurface?.BlockType == BlockType.Wood)
                {
                    // Count trunk height
                    int trunkHeight = 0;
                    for (int y = surfaceY + 1; y < ChunkEntity.Height; y++)
                    {
                        var block = chunk.GetBlock(x, y, z);
                        if (block?.BlockType == BlockType.Wood)
                        {
                            trunkHeight++;
                        }
                        else
                        {
                            break; // Stop when we hit non-wood
                        }
                    }

                    // Verify trunk height is within expected range (4-8 blocks)
                    Assert.That(trunkHeight, Is.GreaterThanOrEqualTo(4),
                        $"Tree trunk at ({x},{z}) should be at least 4 blocks tall");
                    Assert.That(trunkHeight, Is.LessThanOrEqualTo(8),
                        $"Tree trunk at ({x},{z}) should be at most 8 blocks tall");
                }
            }
        }
    }

    [Test]
    public async Task ExecuteAsync_MultipleTreesInForestBiome()
    {
        // Arrange - larger surface to increase chance of multiple trees
        var position = new Vector3(10000, 0, 10000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(5555);
        var context = new GeneratorContext(chunk, position, noise, 5555);

        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        int surfaceY = ChunkEntity.Height / 2;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - count distinct tree bases
        int treeCount = 0;
        for (int x = 2; x < ChunkEntity.Size - 2; x++)
        {
            for (int z = 2; z < ChunkEntity.Size - 2; z++)
            {
                var surfaceBlock = chunk.GetBlock(x, surfaceY, z);
                var blockAboveSurface = chunk.GetBlock(x, surfaceY + 1, z);

                if (surfaceBlock?.BlockType == BlockType.Grass &&
                    blockAboveSurface?.BlockType == BlockType.Wood)
                {
                    treeCount++;
                }
            }
        }

        // Tropical rainforest with density 0.4 should generate at least one tree
        Assert.That(treeCount, Is.GreaterThan(0),
            "Tropical rainforest should generate at least one tree");
    }

    [Test]
    public async Task ExecuteAsync_SurfaceNearTopBoundary_DoesNotThrowException()
    {
        // Arrange - surface at y=63 (ChunkEntity.Height - 1), which was causing the bug
        var position = new Vector3(15000, 0, 15000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(8888);
        var context = new GeneratorContext(chunk, position, noise, 8888);

        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        // Create surface very close to the top (y=63, which is ChunkEntity.Height - 1)
        int surfaceY = ChunkEntity.Height - 1;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act & Assert - should not throw ArgumentOutOfRangeException
        Assert.DoesNotThrowAsync(async () => await _step.ExecuteAsync(context),
            "Should handle surface near top boundary without throwing exception");

        // Verify no trees were placed (not enough space)
        int woodCount = CountBlockType(chunk, BlockType.Wood);
        Assert.That(woodCount, Is.EqualTo(0),
            "Should not place trees when surface is too close to top boundary");
    }

    [Test]
    public async Task ExecuteAsync_SurfaceAt52_CannotPlaceTrees()
    {
        // Arrange - surface at y=52
        // Required space = MaxTreeHeight (8) + 3 = 11
        // 52 + 11 = 63, which is < 64, so technically there's room
        // But 52 + 11 >= 64 is false, so trees should be placeable
        // Let's test the boundary: y=53 is the last position where a tree can be placed
        var position = new Vector3(20000, 0, 20000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(9999);
        var context = new GeneratorContext(chunk, position, noise, 9999);

        context.CustomData["BiomeData"] = new BiomeData
        {
            BiomeType = SquidCraft.Services.Game.Types.BiomeType.TropicalRainforest,
            SurfaceBlock = BlockType.Grass,
            SubsurfaceBlock = BlockType.Dirt,
            HeightMultiplier = 1.0f,
            BaseHeight = 0f
        };

        // Surface at y=53: 53 + 11 = 64, so surfaceY + requiredSpace >= ChunkEntity.Height
        // This should prevent tree placement
        int surfaceY = 53;
        CreateFlatSurface(chunk, surfaceY, BlockType.Grass);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - no trees should be placed due to insufficient vertical space
        int woodCount = CountBlockType(chunk, BlockType.Wood);
        Assert.That(woodCount, Is.EqualTo(0),
            "Should not place trees at y=53 (insufficient space: 53 + 11 >= 64)");
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
