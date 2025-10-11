namespace DemonsGate.Services.Game.Types;

/// <summary>
/// Enumeration of biome types available in the world.
/// Based on the Whittaker biome classification model using temperature and moisture.
/// </summary>
public enum BiomeType
{
    /// <summary>
    /// Ocean biome - deep water.
    /// </summary>
    Ocean,

    /// <summary>
    /// Beach biome - sandy shores.
    /// </summary>
    Beach,

    /// <summary>
    /// Scorched biome - very hot and dry, high elevation.
    /// </summary>
    Scorched,

    /// <summary>
    /// Bare biome - rocky, high elevation with little vegetation.
    /// </summary>
    Bare,

    /// <summary>
    /// Tundra biome - cold, high elevation with snow.
    /// </summary>
    Tundra,

    /// <summary>
    /// Snow biome - permanently frozen, high elevation.
    /// </summary>
    Snow,

    /// <summary>
    /// Temperate desert biome - moderate temperature, very dry.
    /// </summary>
    TemperateDesert,

    /// <summary>
    /// Shrubland biome - moderate temperature and moisture.
    /// </summary>
    Shrubland,

    /// <summary>
    /// Taiga biome - cold forest.
    /// </summary>
    Taiga,

    /// <summary>
    /// Grassland biome - moderate temperature, moderate-low moisture.
    /// </summary>
    Grassland,

    /// <summary>
    /// Temperate deciduous forest biome.
    /// </summary>
    TemperateDeciduousForest,

    /// <summary>
    /// Temperate rainforest biome - moderate temperature, high moisture.
    /// </summary>
    TemperateRainforest,

    /// <summary>
    /// Subtropical desert biome - hot and dry.
    /// </summary>
    SubtropicalDesert,

    /// <summary>
    /// Tropical seasonal forest biome.
    /// </summary>
    TropicalSeasonalForest,

    /// <summary>
    /// Tropical rainforest biome - hot and very wet.
    /// </summary>
    TropicalRainforest,
}
