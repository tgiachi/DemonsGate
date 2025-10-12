using System.Numerics;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;
using SquidCraft.Services.Game.Generation.Noise;
using SquidCraft.Services.Game.Impl.Pipeline;
using SquidCraft.Services.Game.Impl.Pipeline.Steps;
using SquidCraft.Services.Game.Interfaces.Pipeline;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace SquidCraft.Tests.Services.Game;

/// <summary>
/// Integration tests for the complete chunk generation pipeline.
/// </summary>
[TestFixture]
public class GenerationPipelineIntegrationTests
{
    private readonly List<IGeneratorStep> _pipeline;

    public GenerationPipelineIntegrationTests()
    {
        _pipeline =
        [
            new BiomeGeneratorStep(),
            new TerrainGeneratorStep(),
            new CaveGeneratorStep(),
            new TreeGeneratorStep()
        ];
    }

    [Test]
    public async Task FullPipeline_GeneratesCompleteChunk()
    {
        // Arrange
        var position = new Vector3(0, 0, 0);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Act - execute full pipeline
        foreach (var step in _pipeline)
        {
            await step.ExecuteAsync(context);
        }

        // Assert - chunk should have various block types
        var blockTypes = GetUniqueBlockTypes(chunk);
        Assert.That(blockTypes.Count, Is.GreaterThan(1), "Chunk should have multiple block types");
        Assert.That(blockTypes, Has.Member(BlockType.Bedrock));
    }

    [Test]
    public async Task FullPipeline_ExecutesInCorrectOrder()
    {
        // Arrange
        var position = new Vector3(100, 0, 200);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Act & Assert - verify each step executes successfully
        // Step 1: Biome
        await _pipeline[0].ExecuteAsync(context);
        Assert.That(context.CustomData.ContainsKey("BiomeData"), Is.True);

        // Step 2: Terrain
        await _pipeline[1].ExecuteAsync(context);
        bool hasNonAirBlocks = HasBlockType(chunk, BlockType.Dirt) ||
                               HasBlockType(chunk, BlockType.Grass) ||
                               HasBlockType(chunk, BlockType.Stone);
        Assert.That(hasNonAirBlocks, Is.True, "Terrain step should generate blocks");

        // Step 3: Caves
        await _pipeline[2].ExecuteAsync(context);
        // Caves completed successfully (may or may not carve based on noise)

        // Step 4: Trees
        await _pipeline[3].ExecuteAsync(context);
        // Trees completed successfully (may or may not place based on biome/noise)
    }

    [Test]
    public async Task FullPipeline_WithSameSeed_ProducesDeterministicChunks()
    {
        // Arrange
        var position = new Vector3(500, 0, 500);
        var seed = 99999;

        // First chunk
        var chunk1 = new ChunkEntity(position);
        var noise1 = new FastNoiseLite(seed);
        var context1 = new GeneratorContext(chunk1, position, noise1, seed);

        // Second chunk with same seed
        var chunk2 = new ChunkEntity(position);
        var noise2 = new FastNoiseLite(seed);
        var context2 = new GeneratorContext(chunk2, position, noise2, seed);

        // Act - execute full pipeline on both
        foreach (var step in _pipeline)
        {
            await step.ExecuteAsync(context1);
        }

        foreach (var step in _pipeline)
        {
            await step.ExecuteAsync(context2);
        }

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
    public async Task FullPipeline_WithDifferentSeeds_ProducesDifferentChunks()
    {
        // Arrange
        var position = new Vector3(500, 0, 500);

        // First chunk with seed 1
        var chunk1 = new ChunkEntity(position);
        var noise1 = new FastNoiseLite(111);
        var context1 = new GeneratorContext(chunk1, position, noise1, 111);

        // Second chunk with seed 2
        var chunk2 = new ChunkEntity(position);
        var noise2 = new FastNoiseLite(222);
        var context2 = new GeneratorContext(chunk2, position, noise2, 222);

        // Act - execute full pipeline on both
        foreach (var step in _pipeline)
        {
            await step.ExecuteAsync(context1);
        }

        foreach (var step in _pipeline)
        {
            await step.ExecuteAsync(context2);
        }

        // Assert - chunks should be different
        bool foundDifference = false;
        for (int x = 0; x < ChunkEntity.Size && !foundDifference; x++)
        {
            for (int z = 0; z < ChunkEntity.Size && !foundDifference; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block1 = chunk1.GetBlock(x, y, z);
                    var block2 = chunk2.GetBlock(x, y, z);
                    if (block1?.BlockType != block2?.BlockType)
                    {
                        foundDifference = true;
                        break;
                    }
                }
            }
        }

        Assert.That(foundDifference, Is.True, "Different seeds should produce different chunks");
    }

    [Test]
    public async Task FullPipeline_WithMultiplePositions_ProducesDifferentChunks()
    {
        // Arrange
        var seed = 12345;
        var positions = new[]
        {
            new Vector3(0, 0, 0),
            new Vector3(100, 0, 0),
            new Vector3(0, 0, 100),
            new Vector3(500, 0, 500)
        };

        var chunks = new List<ChunkEntity>();

        // Act - generate chunks at different positions
        foreach (var position in positions)
        {
            var chunk = new ChunkEntity(position);
            var noise = new FastNoiseLite(seed);
            var context = new GeneratorContext(chunk, position, noise, seed);

            foreach (var step in _pipeline)
            {
                await step.ExecuteAsync(context);
            }

            chunks.Add(chunk);
        }

        // Assert - at least some chunks should be different from each other
        bool foundDifference = false;
        for (int i = 0; i < chunks.Count - 1 && !foundDifference; i++)
        {
            for (int j = i + 1; j < chunks.Count; j++)
            {
                if (!ChunksAreIdentical(chunks[i], chunks[j]))
                {
                    foundDifference = true;
                    break;
                }
            }
        }

        Assert.That(foundDifference, Is.True, "Chunks at different positions should differ");
    }

    [Test]
    public async Task FullPipeline_GeneratesValidChunkStructure()
    {
        // Arrange
        var position = new Vector3(1000, 0, 1000);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Act
        foreach (var step in _pipeline)
        {
            await step.ExecuteAsync(context);
        }

        // Assert - verify chunk has valid structure
        // 1. Bedrock at bottom
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                var bottomBlock = chunk.GetBlock(x, 0, z);
                Assert.That(bottomBlock, Is.Not.Null);
                Assert.That(bottomBlock.BlockType, Is.EqualTo(BlockType.Bedrock));
            }
        }

        // 2. Has some terrain blocks
        bool hasTerrain = HasBlockType(chunk, BlockType.Dirt) ||
                         HasBlockType(chunk, BlockType.Grass) ||
                         HasBlockType(chunk, BlockType.Stone) ||
                         HasBlockType(chunk, BlockType.Snow);
        Assert.That(hasTerrain, Is.True, "Chunk should have terrain blocks");

        // 3. All blocks are valid (not null)
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    Assert.That(block, Is.Not.Null);
                }
            }
        }
    }


    [TestCase(0, 0, 0)]
    [TestCase(1000, 0, 1000)]
    [TestCase(-1000, 0, -1000)]
    [TestCase(5000, 0, 5000)]
    public async Task FullPipeline_WithVariousPositions_CompletesSuccessfully(
        float x, float y, float z)
    {
        // Arrange
        var position = new Vector3(x, y, z);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Act & Assert - should not throw
        foreach (var step in _pipeline)
        {
            await step.ExecuteAsync(context);
        }

        Assert.That(true, Is.True, "Pipeline completed without errors");
    }

    [Test]
    public async Task FullPipeline_ProducesVariedBiomes()
    {
        // Arrange - generate multiple chunks to see biome variety
        var seed = 12345;
        var biomeTypes = new HashSet<SquidCraft.Services.Game.Types.BiomeType>();

        // Test multiple positions to find different biomes
        for (int i = 0; i < 20; i++)
        {
            var position = new Vector3(i * 1000, 0, i * 1000);
            var chunk = new ChunkEntity(position);
            var noise = new FastNoiseLite(seed);
            var context = new GeneratorContext(chunk, position, noise, seed);

            // Execute biome step
            await _pipeline[0].ExecuteAsync(context);

            if (context.CustomData["BiomeData"] is SquidCraft.Services.Game.Data.BiomeData biomeData)
            {
                biomeTypes.Add(biomeData.BiomeType);
            }
        }

        // Assert - should have generated multiple different biomes
        Assert.That(biomeTypes.Count, Is.GreaterThan(1), "Should generate multiple different biomes across positions");
    }

    private static HashSet<BlockType> GetUniqueBlockTypes(ChunkEntity chunk)
    {
        var types = new HashSet<BlockType>();
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block != null)
                    {
                        types.Add(block.BlockType);
                    }
                }
            }
        }
        return types;
    }

    private static bool HasBlockType(ChunkEntity chunk, BlockType blockType)
    {
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block?.BlockType == blockType)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool ChunksAreIdentical(ChunkEntity chunk1, ChunkEntity chunk2)
    {
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var block1 = chunk1.GetBlock(x, y, z);
                    var block2 = chunk2.GetBlock(x, y, z);
                    if (block1?.BlockType != block2?.BlockType)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
}
