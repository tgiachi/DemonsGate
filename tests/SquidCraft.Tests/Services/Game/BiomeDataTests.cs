using SquidCraft.Game.Data.Types;
using SquidCraft.Services.Game.Data;
using SquidCraft.Services.Game.Types;
using NUnit.Framework;

namespace SquidCraft.Tests.Services.Game;

/// <summary>
/// Unit tests for biome determination and configuration.
/// </summary>
[TestFixture]
public class BiomeDataTests
{
    [TestCase(0.05f, 0.5f, 0.5f, BiomeType.Ocean)]
    [TestCase(0.12f, 0.5f, 0.5f, BiomeType.Beach)]
    [TestCase(0.85f, 0.5f, 0.05f, BiomeType.Scorched)]
    [TestCase(0.85f, 0.5f, 0.15f, BiomeType.Bare)]
    [TestCase(0.85f, 0.5f, 0.4f, BiomeType.Tundra)]
    [TestCase(0.85f, 0.5f, 0.6f, BiomeType.Snow)]
    [TestCase(0.5f, 0.2f, 0.2f, BiomeType.TemperateDesert)]
    [TestCase(0.5f, 0.2f, 0.5f, BiomeType.Shrubland)]
    [TestCase(0.5f, 0.2f, 0.8f, BiomeType.Taiga)]
    [TestCase(0.5f, 0.45f, 0.3f, BiomeType.Grassland)]
    [TestCase(0.5f, 0.5f, 0.6f, BiomeType.TemperateDeciduousForest)]
    [TestCase(0.5f, 0.5f, 0.8f, BiomeType.TemperateRainforest)]
    [TestCase(0.5f, 0.8f, 0.2f, BiomeType.SubtropicalDesert)]
    [TestCase(0.5f, 0.8f, 0.4f, BiomeType.TropicalSeasonalForest)]
    [TestCase(0.5f, 0.9f, 0.8f, BiomeType.TropicalRainforest)]
    public void DetermineBiome_WithVariousConditions_ReturnsExpectedBiome(
        float elevation, float temperature, float moisture, BiomeType expectedBiome)
    {
        // Act
        var result = BiomeData.DetermineBiome(elevation, temperature, moisture);

        // Assert
        Assert.That(result, Is.EqualTo(expectedBiome));
    }

    
    [TestCase(BiomeType.Ocean, BlockType.Water, BlockType.Dirt)]
    [TestCase(BiomeType.Beach, BlockType.Grass, BlockType.Dirt)]
    [TestCase(BiomeType.Scorched, BlockType.Stone, BlockType.Stone)]
    [TestCase(BiomeType.Bare, BlockType.Stone, BlockType.Stone)]
    [TestCase(BiomeType.Snow, BlockType.Snow, BlockType.Ice)]
    [TestCase(BiomeType.TropicalRainforest, BlockType.Moss, BlockType.Dirt)]
    [TestCase(BiomeType.TemperateRainforest, BlockType.Moss, BlockType.Dirt)]
    [TestCase(BiomeType.TemperateDeciduousForest, BlockType.Grass, BlockType.Dirt)]
    [TestCase(BiomeType.TropicalSeasonalForest, BlockType.Grass, BlockType.Dirt)]
    [TestCase(BiomeType.Taiga, BlockType.Grass, BlockType.Dirt)]
    [TestCase(BiomeType.Tundra, BlockType.Snow, BlockType.Dirt)]
    [TestCase(BiomeType.Grassland, BlockType.Grass, BlockType.Dirt)]
    [TestCase(BiomeType.Shrubland, BlockType.Grass, BlockType.Dirt)]
    [TestCase(BiomeType.SubtropicalDesert, BlockType.Grass, BlockType.Dirt)]
    [TestCase(BiomeType.TemperateDesert, BlockType.Grass, BlockType.Dirt)]
    public void GetBiomeConfiguration_ForAllBiomes_ReturnsValidConfiguration(
        BiomeType biomeType, BlockType expectedSurface, BlockType expectedSubsurface)
    {
        // Act
        var config = BiomeData.GetBiomeConfiguration(biomeType);

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.SurfaceBlock, Is.EqualTo(expectedSurface));
        Assert.That(config.SubsurfaceBlock, Is.EqualTo(expectedSubsurface));
        Assert.That(config.HeightMultiplier, Is.GreaterThan(0f));
        Assert.That(config.BaseHeight, Is.InRange(-50f, 100f));
    }

    
    [TestCase(BiomeType.TropicalRainforest, 1.0f, 15f)]
    [TestCase(BiomeType.TemperateRainforest, 1.1f, 18f)]
    [TestCase(BiomeType.TemperateDeciduousForest, 0.9f, 12f)]
    [TestCase(BiomeType.SubtropicalDesert, 0.5f, 5f)]
    [TestCase(BiomeType.TemperateDesert, 0.6f, 5f)]
    [TestCase(BiomeType.Snow, 1.5f, 45f)]
    public void GetBiomeConfiguration_ForBiome_ReturnsExpectedHeightModifiers(
        BiomeType biomeType, float expectedHeightMultiplier, float expectedBaseHeight)
    {
        // Act
        var config = BiomeData.GetBiomeConfiguration(biomeType);

        // Assert
        Assert.That(config.HeightMultiplier, Is.EqualTo(expectedHeightMultiplier));
        Assert.That(config.BaseHeight, Is.EqualTo(expectedBaseHeight));
    }

    [Test]
    public void DetermineBiome_AtOceanDepth_ReturnsOcean()
    {
        // Arrange - very low elevation, any temperature/moisture
        float elevation = 0.05f;

        // Act
        var result = BiomeData.DetermineBiome(elevation, 0.5f, 0.5f);

        // Assert
        Assert.That(result, Is.EqualTo(BiomeType.Ocean));
    }

    [Test]
    public void DetermineBiome_AtHighElevationLowTemp_ReturnsSnow()
    {
        // Arrange - high elevation, low temperature
        float elevation = 0.85f;
        float temperature = 0.2f;

        // Act
        var result = BiomeData.DetermineBiome(elevation, temperature, 0.5f);

        // Assert
        Assert.That(result, Is.EqualTo(BiomeType.Snow));
    }

    [Test]
    public void DetermineBiome_AtHighElevationHighTemp_ReturnsTundra()
    {
        // Arrange - high elevation, higher temperature
        float elevation = 0.85f;
        float temperature = 0.5f;

        // Act
        var result = BiomeData.DetermineBiome(elevation, temperature, 0.4f);

        // Assert
        Assert.That(result, Is.EqualTo(BiomeType.Tundra));
    }

    
    [TestCase(0.9f, 0.9f, BiomeType.TropicalRainforest)]
    [TestCase(0.9f, 0.1f, BiomeType.SubtropicalDesert)]
    [TestCase(0.2f, 0.8f, BiomeType.Taiga)]
    [TestCase(0.2f, 0.2f, BiomeType.TemperateDesert)]
    public void DetermineBiome_WithExtremeTemperatureAndMoisture_ReturnsExpectedBiome(
        float temperature, float moisture, BiomeType expectedBiome)
    {
        // Arrange - mid elevation to avoid ocean/mountain biomes
        float elevation = 0.5f;

        // Act
        var result = BiomeData.DetermineBiome(elevation, temperature, moisture);

        // Assert
        Assert.That(result, Is.EqualTo(expectedBiome));
    }
}
