using DemonsGate.Game.Data.Types;

namespace DemonsGate.Services.Game.Data;

/// <summary>
/// Configuration data for a biome.
/// </summary>
public class BiomeConfiguration
{
    public BlockType SurfaceBlock { get; set; }
    public BlockType SubsurfaceBlock { get; set; }
    public float HeightMultiplier { get; set; }
    public float BaseHeight { get; set; }
}
