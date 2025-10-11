using DemonsGate.Game.Data.Primitives;
using DemonsGate.Services.Game.Interfaces.Pipeline;
using DemonsGate.Services.Game.Types;
using Serilog;

namespace DemonsGate.Services.Game.Impl.Pipeline.Steps;

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
    private const float CaveThreshold = 0.6f;

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
        var noise = context.NoiseGenerator;

        // Iterate through all blocks in the chunk
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                for (int y = MinCaveY; y < ChunkEntity.Height && y < MaxCaveY; y++)
                {
                    // Calculate world coordinates
                    float worldX = (worldPos.X + x) * NoiseScale;
                    float worldY = (worldPos.Y + y) * NoiseScale;
                    float worldZ = (worldPos.Z + z) * NoiseScale;

                    // Get 3D noise value
                    float noiseValue = noise.GetNoise(worldX, worldY, worldZ);

                    // Normalize noise from [-1, 1] to [0, 1]
                    float normalizedNoise = (noiseValue + 1f) * 0.5f;

                    // If noise is above threshold, carve out the block (make it air)
                    if (normalizedNoise > CaveThreshold)
                    {
                        var currentBlock = chunk.GetBlock(x, y, z);

                        // Only carve out solid blocks (don't affect air or bedrock)
                        if (currentBlock != null &&
                            currentBlock.BlockType != BlockType.Air &&
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
}
