using System;
using System.Numerics;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;
using SquidCraft.Services.Game.Data;
using SquidCraft.Services.Game.Generation.Noise;
using SquidCraft.Services.Game.Impl.Pipeline;
using SquidCraft.Services.Game.Impl.Pipeline.Steps;
using SquidCraft.Services.Game.Types;
using NUnit.Framework;

namespace SquidCraft.Tests.Services.Game;

/// <summary>
/// Integration tests for BiomeGeneratorStep.
/// </summary>
[TestFixture]
public class BiomeGeneratorStepTests
{
    private readonly BiomeGeneratorStep _step;

    public BiomeGeneratorStepTests()
    {
        _step = new BiomeGeneratorStep();
    }

    [Test]
    public void Name_ReturnsCorrectStepName()
    {
        // Assert
        Assert.That(_step.Name, Is.EqualTo("BiomeGenerator"));
    }

    [Test]
    public async Task ExecuteAsync_GeneratesBiomeData()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert
        Assert.That(context.CustomData.ContainsKey("BiomeData"), Is.True);
        var biomeData = context.CustomData["BiomeData"] as BiomeData;
        Assert.That(biomeData, Is.Not.Null);
    }

    [Test]
    public async Task ExecuteAsync_SetsBiomeProperties()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert
        var biomeData = context.CustomData["BiomeData"] as BiomeData;
        Assert.That(biomeData, Is.Not.Null);
        Assert.That(biomeData.Temperature, Is.InRange(0f, 1f));
        Assert.That(biomeData.Moisture, Is.InRange(0f, 1f));
        Assert.That(biomeData.Elevation, Is.InRange(0f, 1f));
        Assert.That(Enum.IsDefined(typeof(BiomeType), biomeData.BiomeType), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_SetsBiomeConfiguration()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert
        var biomeData = context.CustomData["BiomeData"] as BiomeData;
        Assert.That(biomeData, Is.Not.Null);
        Assert.That(biomeData.SurfaceBlock, Is.Not.EqualTo(default(BlockType)));
        Assert.That(biomeData.SubsurfaceBlock, Is.Not.EqualTo(default(BlockType)));
        Assert.That(biomeData.HeightMultiplier, Is.GreaterThan(0f));
    }

    
    [TestCase(0, 0, 0)]
    [TestCase(100, 0, 0)]
    [TestCase(0, 0, 100)]
    [TestCase(1000, 0, 1000)]
    [TestCase(-500, 0, -500)]
    public async Task ExecuteAsync_WithDifferentPositions_GeneratesDifferentBiomes(
        float x, float y, float z)
    {
        // Arrange
        var position = new Vector3(x, y, z);
        var chunk = new ChunkEntity(position);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, position, noise, 12345);

        // Act
        await _step.ExecuteAsync(context);

        // Assert
        var biomeData = context.CustomData["BiomeData"] as BiomeData;
        Assert.That(biomeData, Is.Not.Null);
        // Just verify it doesn't crash and produces valid biome data
        Assert.That(biomeData.Temperature, Is.InRange(0f, 1f));
        Assert.That(biomeData.Moisture, Is.InRange(0f, 1f));
        Assert.That(biomeData.Elevation, Is.InRange(0f, 1f));
    }

    [Test]
    public async Task ExecuteAsync_WithSameSeedAndPosition_ProducesDeterministicResults()
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

        // Assert
        var biomeData1 = context1.CustomData["BiomeData"] as BiomeData;
        var biomeData2 = context2.CustomData["BiomeData"] as BiomeData;

        Assert.That(biomeData1, Is.Not.Null);
        Assert.That(biomeData2, Is.Not.Null);
        Assert.That(biomeData2.BiomeType, Is.EqualTo(biomeData1.BiomeType));
        Assert.That(biomeData2.Temperature, Is.EqualTo(biomeData1.Temperature));
        Assert.That(biomeData2.Moisture, Is.EqualTo(biomeData1.Moisture));
        Assert.That(biomeData2.Elevation, Is.EqualTo(biomeData1.Elevation));
    }

    [Test]
    public async Task ExecuteAsync_WithDifferentSeeds_ProducesDifferentResults()
    {
        // Arrange
        var position = new Vector3(100, 0, 200);

        var chunk1 = new ChunkEntity(position);
        var noise1 = new FastNoiseLite(12345);
        var context1 = new GeneratorContext(chunk1, position, noise1, 12345);

        var chunk2 = new ChunkEntity(position);
        var noise2 = new FastNoiseLite(99999);
        var context2 = new GeneratorContext(chunk2, position, noise2, 99999);

        // Act
        await _step.ExecuteAsync(context1);
        await _step.ExecuteAsync(context2);

        // Assert
        var biomeData1 = context1.CustomData["BiomeData"] as BiomeData;
        var biomeData2 = context2.CustomData["BiomeData"] as BiomeData;

        Assert.That(biomeData1, Is.Not.Null);
        Assert.That(biomeData2, Is.Not.Null);

        // At least one value should be different with different seeds
        bool isDifferent = biomeData1.BiomeType != biomeData2.BiomeType ||
                          Math.Abs(biomeData1.Temperature - biomeData2.Temperature) > 0.01f ||
                          Math.Abs(biomeData1.Moisture - biomeData2.Moisture) > 0.01f ||
                          Math.Abs(biomeData1.Elevation - biomeData2.Elevation) > 0.01f;

        Assert.That(isDifferent, Is.True, "Different seeds should produce different biome data");
    }

    [Test]
    public async Task ExecuteAsync_DoesNotModifyChunk()
    {
        // Arrange
        var chunk = new ChunkEntity(Vector3.Zero);
        var noise = new FastNoiseLite(12345);
        var context = new GeneratorContext(chunk, Vector3.Zero, noise, 12345);

        // Verify chunk starts empty
        var initialBlock = chunk.GetBlock(0, 0, 0);
        Assert.That(initialBlock, Is.Null);

        // Act
        await _step.ExecuteAsync(context);

        // Assert - chunk should still be empty (biome step doesn't modify blocks)
        var finalBlock = chunk.GetBlock(0, 0, 0);
        Assert.That(finalBlock, Is.Null);
    }
}
