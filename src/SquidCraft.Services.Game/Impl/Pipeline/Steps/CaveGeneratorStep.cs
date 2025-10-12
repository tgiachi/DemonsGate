using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;
using SquidCraft.Services.Game.Interfaces.Pipeline;
using Serilog;

namespace SquidCraft.Services.Game.Impl.Pipeline.Steps;

/// <summary>
/// Generates caves and underground caverns using 3D noise.
/// </summary>
public class CaveGeneratorStep : IGeneratorStep
{
    private readonly ILogger _logger = Log.ForContext<CaveGeneratorStep>();

    /// <summary>
    /// The threshold above which blocks are carved out to create caves.
    /// Higher values create smaller caves, lower values create larger caves.
    /// Typical range: 0.5 to 0.7
    /// </summary>
    private const float CaveThreshold = 0.55f;

    /// <summary>
    /// Minimum Y level where caves can generate.
    /// </summary>
    private const int MinCaveY = 1;

    /// <summary>
    /// Maximum Y level where caves can generate.
    /// Caves won't generate above this level.
    /// </summary>
    private const int MaxCaveY = 128;

    /// <summary>
    /// Scale factor for the 3D noise. Lower values create larger, more spread out caves.
    /// </summary>
    private const float NoiseScale = 0.05f;

    /// <inheritdoc/>
    public string Name => "CaveGenerator";

    /// <inheritdoc/>
    public Task ExecuteAsync(IGeneratorContext context)
    {
        _logger.Debug("Generating caves for chunk at {Position}", context.WorldPosition);

        var chunk = context.Chunk;
        var worldPos = context.WorldPosition;

        // Create a configured noise generator for 3D cave generation
        var noise = CreateCaveNoiseGenerator(context.Seed);

        // Iterate through all blocks in the chunk
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = MinCaveY; y < ChunkEntity.Height && y < MaxCaveY; y++)
                {
                    // Calculate world coordinates
                    float worldX = worldPos.X + x;
                    float worldY = worldPos.Y + y;
                    float worldZ = worldPos.Z + z;

                    // Get 3D noise value
                    float noiseValue = noise.GetNoise(worldX, worldY, worldZ);

                    // Normalize noise from [-1, 1] to [0, 1]
                    float normalizedNoise = (noiseValue + 1f) * 0.5f;

                    // If noise is above threshold, carve out the block (make it air)
                    if (normalizedNoise > CaveThreshold)
                    {
                        var currentBlock = chunk.GetBlock(x, y, z);

                        // Only carve out solid blocks (don't affect air, water, or bedrock)
                        if (currentBlock != null &&
                            currentBlock.BlockType != BlockType.Air &&
                            currentBlock.BlockType != BlockType.Water &&
                            currentBlock.BlockType != BlockType.Bedrock)
                        {
                            // Replace with air to create cave
                            chunk.SetBlock(x, y, z, new BlockEntity(currentBlock.Id, BlockType.Air));
                        }
                    }
                }
            }
        }

        _logger.Debug("Cave generation completed for chunk at {Position}", context.WorldPosition);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a properly configured noise generator for cave generation.
    /// </summary>
    private static Generation.Noise.FastNoiseLite CreateCaveNoiseGenerator(int seed)
    {
        var noise = new Generation.Noise.FastNoiseLite(seed);
        noise.SetNoiseType(Generation.Noise.NoiseType.OpenSimplex2);
        noise.SetFrequency(NoiseScale);
        noise.SetFractalType(Generation.Noise.FractalType.FBm);
        noise.SetFractalOctaves(2);
        return noise;
    }
}
