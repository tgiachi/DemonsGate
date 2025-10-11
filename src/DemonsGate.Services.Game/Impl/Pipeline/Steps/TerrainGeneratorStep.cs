using DemonsGate.Game.Data.Primitives;
using DemonsGate.Game.Data.Types;
using DemonsGate.Services.Game.Interfaces.Pipeline;
using Serilog;

namespace DemonsGate.Services.Game.Impl.Pipeline.Steps;

/// <summary>
/// Generates basic terrain using noise-based height maps.
/// </summary>
public class TerrainGeneratorStep : IGeneratorStep
{
    private readonly ILogger _logger = Log.ForContext<TerrainGeneratorStep>();

    /// <summary>
    /// The world Y coordinate where bedrock layer is placed.
    /// </summary>
    private const int BedrockWorldY = -1024;

    /// <summary>
    /// The sea level Y coordinate. Water will be generated up to this level.
    /// </summary>
    private const int SeaLevel = 64;

    /// <summary>
    /// Depth at which stone starts to replace dirt.
    /// </summary>
    private const int StoneDepth = 5;

    /// <inheritdoc/>
    public string Name => "TerrainGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        _logger.Debug("Generating terrain for chunk at {Position}", context.WorldPosition);

        var chunk = context.Chunk;
        var worldPos = context.WorldPosition;
        var noise = context.NoiseGenerator;

        // Get biome data if available (from BiomeGeneratorStep)
        var biomeData = context.CustomData.TryGetValue("BiomeData", out var biomeObj)
            ? biomeObj as Data.BiomeData
            : null;

        // Use biome configuration or defaults
        var surfaceBlock = biomeData?.SurfaceBlock ?? BlockType.Grass;
        var subsurfaceBlock = biomeData?.SubsurfaceBlock ?? BlockType.Dirt;
        var heightMultiplier = biomeData?.HeightMultiplier ?? 1.0f;
        var baseHeight = biomeData?.BaseHeight ?? 0f;

        // Generate terrain using 2D noise for height mapping
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                // Calculate world coordinates
                float worldX = worldPos.X + x;
                float worldZ = worldPos.Z + z;

                // Get noise value (-1 to 1) and convert to height (0 to ChunkEntity.Height)
                float noiseValue = noise.GetNoise(worldX, worldZ);

                // Apply biome-specific height modifications
                int terrainHeight = (int)((noiseValue + 1f) * 0.5f * ChunkEntity.Height * 0.6f * heightMultiplier + baseHeight);

                // Clamp height to valid range
                terrainHeight = Math.Clamp(terrainHeight, 1, ChunkEntity.Height - 1);

                // Fill blocks from bottom to terrain height
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    // Calculate world Y coordinate
                    int worldY = (int)worldPos.Y + y;

                    BlockType blockType;

                    if (worldY == BedrockWorldY)
                    {
                        // Bedrock at the configured world Y level
                        blockType = BlockType.Bedrock;
                    }
                    else if (y == 0)
                    {
                        // Bedrock at the bottom of the chunk
                        blockType = BlockType.Bedrock;
                    }
                    else if (y < terrainHeight - StoneDepth)
                    {
                        // Deep underground is stone
                        blockType = BlockType.Stone;
                    }
                    else if (y < terrainHeight)
                    {
                        // Near surface uses biome-specific subsurface block
                        blockType = subsurfaceBlock;
                    }
                    else if (y == terrainHeight)
                    {
                        // Surface uses biome-specific surface block (if above sea level) or subsurface (if underwater)
                        blockType = y >= SeaLevel ? surfaceBlock : subsurfaceBlock;
                    }
                    else if (y < SeaLevel)
                    {
                        // Below sea level but above terrain is water
                        blockType = BlockType.Water;
                    }
                    else
                    {
                        // Above terrain and above sea level is air
                        blockType = BlockType.Air;
                    }

                    // Create block with a unique ID based on world position
                    long blockId = GenerateBlockId(worldPos, x, y, z);
                    chunk.SetBlock(x, y, z, new BlockEntity(blockId, blockType));
                }
            }
        }

        _logger.Debug("Terrain generation completed for chunk at {Position}", context.WorldPosition);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Generates a unique block ID based on world position and local coordinates.
    /// </summary>
    private long GenerateBlockId(System.Numerics.Vector3 worldPos, int x, int y, int z)
    {
        // Simple hash-based ID generation
        long wx = (long)worldPos.X;
        long wy = (long)worldPos.Y;
        long wz = (long)worldPos.Z;

        return (wx << 48) | (wy << 32) | (wz << 16) | ((long)x << 12) | ((long)y << 6) | (long)z;
    }
}
