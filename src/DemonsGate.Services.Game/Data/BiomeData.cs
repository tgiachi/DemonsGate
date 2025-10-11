using DemonsGate.Game.Data.Types;
using DemonsGate.Services.Game.Types;

namespace DemonsGate.Services.Game.Data;

/// <summary>
/// Contains biome-specific data including temperature, moisture, and terrain properties.
/// </summary>
public class BiomeData
{
    /// <summary>
    /// The type of biome.
    /// </summary>
    public BiomeType BiomeType { get; set; }

    /// <summary>
    /// Temperature value (0.0 = cold, 1.0 = hot).
    /// </summary>
    public float Temperature { get; set; }

    /// <summary>
    /// Moisture value (0.0 = dry, 1.0 = wet).
    /// </summary>
    public float Moisture { get; set; }

    /// <summary>
    /// Elevation value (0.0 = low, 1.0 = high).
    /// </summary>
    public float Elevation { get; set; }

    /// <summary>
    /// Surface block type for this biome.
    /// </summary>
    public BlockType SurfaceBlock { get; set; }

    /// <summary>
    /// Subsurface block type (below surface).
    /// </summary>
    public BlockType SubsurfaceBlock { get; set; }

    /// <summary>
    /// Height multiplier for terrain generation (affects mountain/valley height).
    /// </summary>
    public float HeightMultiplier { get; set; }

    /// <summary>
    /// Base height offset for this biome.
    /// </summary>
    public float BaseHeight { get; set; }

    public BiomeData()
    {
        BiomeType = BiomeType.Grassland;
        Temperature = 0.5f;
        Moisture = 0.5f;
        Elevation = 0.5f;
        SurfaceBlock = BlockType.Grass;
        SubsurfaceBlock = BlockType.Dirt;
        HeightMultiplier = 1.0f;
        BaseHeight = 0.0f;
    }

    /// <summary>
    /// Determines the biome type based on temperature, moisture, and elevation.
    /// Uses the Whittaker biome classification model.
    /// </summary>
    public static BiomeType DetermineBiome(float elevation, float temperature, float moisture)
    {
        // Ocean and Beach (low elevation)
        if (elevation < 0.1f) return BiomeType.Ocean;
        if (elevation < 0.15f) return BiomeType.Beach;

        // High elevation biomes (mountains)
        if (elevation > 0.8f)
        {
            if (moisture < 0.1f) return BiomeType.Scorched;
            if (moisture < 0.2f) return BiomeType.Bare;
            if (moisture < 0.5f) return BiomeType.Tundra;
            return BiomeType.Snow;
        }

        // Cold biomes (low temperature)
        if (temperature < 0.3f)
        {
            if (moisture < 0.3f) return BiomeType.TemperateDesert;
            if (moisture < 0.6f) return BiomeType.Shrubland;
            return BiomeType.Taiga;
        }

        // Temperate biomes (medium temperature)
        if (temperature < 0.6f)
        {
            if (moisture < 0.2f) return BiomeType.TemperateDesert;
            if (moisture < 0.4f) return BiomeType.Grassland;
            if (moisture < 0.7f) return BiomeType.TemperateDeciduousForest;
            return BiomeType.TemperateRainforest;
        }

        // Hot biomes (high temperature)
        if (moisture < 0.3f) return BiomeType.SubtropicalDesert;
        if (moisture < 0.6f) return BiomeType.TropicalSeasonalForest;
        return BiomeType.TropicalRainforest;
    }

    /// <summary>
    /// Gets biome-specific configuration data.
    /// </summary>
    public static BiomeConfiguration GetBiomeConfiguration(BiomeType biomeType)
    {
        return biomeType switch
        {
            BiomeType.Ocean => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Water,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.2f,
                BaseHeight = -20f
            },
            BiomeType.Beach => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.3f,
                BaseHeight = 0f
            },
            BiomeType.Scorched => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Stone,
                SubsurfaceBlock = BlockType.Stone,
                HeightMultiplier = 1.5f,
                BaseHeight = 40f
            },
            BiomeType.Bare => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Stone,
                SubsurfaceBlock = BlockType.Stone,
                HeightMultiplier = 1.4f,
                BaseHeight = 35f
            },
            BiomeType.Tundra => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Snow,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 1.3f,
                BaseHeight = 30f
            },
            BiomeType.Snow => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Snow,
                SubsurfaceBlock = BlockType.Ice,
                HeightMultiplier = 1.5f,
                BaseHeight = 45f
            },
            BiomeType.TemperateDesert => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.6f,
                BaseHeight = 5f
            },
            BiomeType.Shrubland => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.8f,
                BaseHeight = 8f
            },
            BiomeType.Taiga => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 1.0f,
                BaseHeight = 15f
            },
            BiomeType.Grassland => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.7f,
                BaseHeight = 10f
            },
            BiomeType.TemperateDeciduousForest => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.9f,
                BaseHeight = 12f
            },
            BiomeType.TemperateRainforest => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Moss,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 1.1f,
                BaseHeight = 18f
            },
            BiomeType.SubtropicalDesert => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.5f,
                BaseHeight = 5f
            },
            BiomeType.TropicalSeasonalForest => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 0.8f,
                BaseHeight = 10f
            },
            BiomeType.TropicalRainforest => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Moss,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 1.0f,
                BaseHeight = 15f
            },
            _ => new BiomeConfiguration
            {
                SurfaceBlock = BlockType.Grass,
                SubsurfaceBlock = BlockType.Dirt,
                HeightMultiplier = 1.0f,
                BaseHeight = 10f
            }
        };
    }
}
