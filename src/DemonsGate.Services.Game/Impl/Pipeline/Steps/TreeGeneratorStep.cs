using DemonsGate.Game.Data.Primitives;
using DemonsGate.Game.Data.Types;
using DemonsGate.Services.Game.Data;
using DemonsGate.Services.Game.Generation.Noise;
using DemonsGate.Services.Game.Interfaces.Pipeline;
using DemonsGate.Services.Game.Types;
using Serilog;

namespace DemonsGate.Services.Game.Impl.Pipeline.Steps;

/// <summary>
/// Generates trees based on biome data.
/// </summary>
public class TreeGeneratorStep : IGeneratorStep
{
    private readonly ILogger _logger = Log.ForContext<TreeGeneratorStep>();

    /// <summary>
    /// Scale for tree placement noise (lower = larger clusters of trees).
    /// </summary>
    private const float TreePlacementScale = 0.1f;

    /// <summary>
    /// Minimum height for trees.
    /// </summary>
    private const int MinTreeHeight = 4;

    /// <summary>
    /// Maximum height for trees.
    /// </summary>
    private const int MaxTreeHeight = 8;

    /// <summary>
    /// Sea level - trees won't generate below this.
    /// </summary>
    private const int SeaLevel = 64;

    /// <inheritdoc/>
    public string Name => "TreeGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        _logger.Debug("Generating trees for chunk at {Position}", context.WorldPosition);

        var chunk = context.Chunk;
        var worldPos = context.WorldPosition;
        var seed = context.Seed;

        // Get biome data
        var biomeData = context.CustomData.TryGetValue("BiomeData", out var biomeObj)
            ? biomeObj as BiomeData
            : null;

        if (biomeData == null)
        {
            _logger.Debug("No biome data found, skipping tree generation");
            return Task.CompletedTask;
        }

        // Get tree density and threshold for this biome
        var treeDensity = GetTreeDensity(biomeData.BiomeType);

        _logger.Debug(
            "Tree generation for biome {BiomeType}: density threshold = {Threshold}",
            biomeData.BiomeType,
            treeDensity
        );

        if (treeDensity <= 0f)
        {
            _logger.Debug("Biome {BiomeType} does not support trees", biomeData.BiomeType);
            return Task.CompletedTask;
        }

        // Create noise generator for tree placement
        var treeNoise = CreateTreePlacementNoise(seed);

        int treesGenerated = 0;
        int noisePassCount = 0;
        int surfaceFoundCount = 0;
        int canPlaceFailCount = 0;

        // Check each position in the chunk for potential tree placement
        for (int x = 2; x < ChunkEntity.Size - 2; x++) // Leave margin for tree canopy
        {
            for (int z = 2; z < ChunkEntity.Size - 2; z++)
            {
                float worldX = worldPos.X + x;
                float worldZ = worldPos.Z + z;

                // Sample noise to determine if a tree should be placed here
                float noiseValue = (treeNoise.GetNoise(worldX, worldZ) + 1f) * 0.5f; // Normalize to [0,1]

                if (noiseValue > treeDensity)
                {
                    noisePassCount++;

                    // Find the surface block at this position
                    int surfaceY = FindSurfaceY(chunk, x, z);

                    if (surfaceY >= 0)
                    {
                        surfaceFoundCount++;

                        if (CanPlaceTree(chunk, x, surfaceY, z, out string failReason))
                        {
                            int treeHeight = GetTreeHeight(seed, x, z);
                            GenerateTree(chunk, x, surfaceY, z, treeHeight);
                            treesGenerated++;
                        }
                        else
                        {
                            canPlaceFailCount++;
                            if (canPlaceFailCount <= 3) // Log first few failures
                            {
                                _logger.Debug("Tree placement failed at ({X},{Y},{Z}): {Reason}", x, surfaceY, z, failReason);
                            }
                        }
                    }
                }
            }
        }

        _logger.Debug(
            "Tree generation stats: noise passed={NoisePass}, surface found={SurfaceFound}, can place failed={CanPlaceFail}, trees generated={TreesGenerated}",
            noisePassCount,
            surfaceFoundCount,
            canPlaceFailCount,
            treesGenerated
        );

        _logger.Debug("Generated {TreeCount} trees in chunk at {Position}", treesGenerated, context.WorldPosition);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the tree density threshold for a biome (0.0 = no trees, 1.0 = rare trees, lower = more trees).
    /// </summary>
    private static float GetTreeDensity(BiomeType biomeType)
    {
        return biomeType switch
        {
            BiomeType.TropicalRainforest => 0.4f,        // Very dense
            BiomeType.TemperateRainforest => 0.45f,      // Very dense
            BiomeType.TemperateDeciduousForest => 0.5f,  // Dense
            BiomeType.TropicalSeasonalForest => 0.55f,   // Dense
            BiomeType.Taiga => 0.6f,                     // Moderate
            BiomeType.Shrubland => 0.75f,                // Sparse
            BiomeType.Grassland => 0.85f,                // Very sparse
            BiomeType.Tundra => 0.9f,                    // Extremely sparse
            _ => 0f                                       // No trees
        };
    }

    /// <summary>
    /// Creates a noise generator for tree placement.
    /// </summary>
    private static FastNoiseLite CreateTreePlacementNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + 5000);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(TreePlacementScale);
        return noise;
    }

    /// <summary>
    /// Finds the Y coordinate of the surface at the given X,Z position.
    /// </summary>
    private static int FindSurfaceY(ChunkEntity chunk, int x, int z)
    {
        // Search from top to bottom
        for (int y = ChunkEntity.Height - 1; y >= 0; y--)
        {
            var block = chunk.GetBlock(x, y, z);
            if (block != null && block.BlockType != BlockType.Air && block.BlockType != BlockType.Water)
            {
                return y;
            }
        }
        return -1;
    }

    /// <summary>
    /// Checks if a tree can be placed at the given position.
    /// </summary>
    private static bool CanPlaceTree(ChunkEntity chunk, int x, int surfaceY, int z, out string failReason)
    {
        // Check if surface block is suitable for trees
        var surfaceBlock = chunk.GetBlock(x, surfaceY, z);
        if (surfaceBlock == null)
        {
            failReason = "Surface block is null";
            return false;
        }

        var blockType = surfaceBlock.BlockType;

        // Trees can grow on grass, dirt, or moss
        if (blockType != BlockType.Grass && blockType != BlockType.Dirt && blockType != BlockType.Moss)
        {
            failReason = $"Invalid surface block type: {blockType}";
            return false;
        }

        // Check if underwater (block above surface is water)
        var blockAbove = chunk.GetBlock(x, surfaceY + 1, z);
        if (blockAbove != null && blockAbove.BlockType == BlockType.Water)
        {
            failReason = "Underwater (block above is water)";
            return false;
        }

        // Check if there's enough space above for the tree
        int requiredSpace = MaxTreeHeight + 3; // Tree height + canopy space
        if (surfaceY + requiredSpace >= ChunkEntity.Height)
        {
            failReason = $"Not enough vertical space (surfaceY={surfaceY}, required={requiredSpace}, height={ChunkEntity.Height})";
            return false;
        }

        // Check if the space above is air
        for (int y = surfaceY + 1; y <= surfaceY + requiredSpace; y++)
        {
            var block = chunk.GetBlock(x, y, z);
            if (block != null && block.BlockType != BlockType.Air)
            {
                failReason = $"Non-air block at Y={y} (type={block.BlockType})";
                return false;
            }
        }

        failReason = string.Empty;
        return true;
    }

    /// <summary>
    /// Gets a pseudo-random tree height based on position.
    /// </summary>
    private static int GetTreeHeight(int seed, int x, int z)
    {
        // Simple hash to get deterministic random height
        int hash = seed + (x * 374761393) + (z * 668265263);
        hash = (hash ^ (hash >> 13)) * 1274126177;
        int height = (hash & 0xFF) % (MaxTreeHeight - MinTreeHeight + 1) + MinTreeHeight;
        return height;
    }

    /// <summary>
    /// Generates a tree at the specified position.
    /// </summary>
    private void GenerateTree(ChunkEntity chunk, int x, int surfaceY, int z, int height)
    {
        // Generate trunk
        for (int y = 1; y <= height; y++)
        {
            var block = chunk.GetBlock(x, surfaceY + y, z);
            if (block != null)
            {
                chunk.SetBlock(x, surfaceY + y, z, new BlockEntity(block.Id, BlockType.Wood));
            }
        }

        // Generate canopy (simple sphere-like shape)
        int canopyY = surfaceY + height;
        int canopyRadius = 2;

        for (int dy = -1; dy <= 2; dy++)
        {
            for (int dx = -canopyRadius; dx <= canopyRadius; dx++)
            {
                for (int dz = -canopyRadius; dz <= canopyRadius; dz++)
                {
                    // Skip the center (where the trunk is at the top)
                    if (dx == 0 && dz == 0 && dy >= 0)
                        continue;

                    // Calculate distance from center
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    // Place leaves if within canopy radius
                    if (distance <= canopyRadius + 0.5f)
                    {
                        int lx = x + dx;
                        int ly = canopyY + dy;
                        int lz = z + dz;

                        // Check bounds
                        if (lx >= 0 && lx < ChunkEntity.Size &&
                            ly >= 0 && ly < ChunkEntity.Height &&
                            lz >= 0 && lz < ChunkEntity.Size)
                        {
                            var block = chunk.GetBlock(lx, ly, lz);
                            if (block != null && block.BlockType == BlockType.Air)
                            {
                                chunk.SetBlock(lx, ly, lz, new BlockEntity(block.Id, BlockType.Leaves));
                            }
                        }
                    }
                }
            }
        }
    }
}
