using System.Numerics;

namespace DemonsGate.Services.Game.Data.Config;

/// <summary>
/// Configuration settings for the chunk generator service.
/// </summary>
public class ChunkGeneratorConfig
{
    /// <summary>
    /// Gets or sets the seed for procedural generation.
    /// If null, a random seed will be generated.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Gets or sets the cache expiration time in minutes.
    /// Default is 5 minutes.
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the initial position for chunk generation.
    /// This is the center point around which initial chunks will be generated.
    /// Default is Vector3.Zero (0, 0, 0).
    /// </summary>
    public Vector3 InitialPosition { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the number of chunks to generate in each direction from the initial position.
    /// Default is 5 (generates a 11x11 area around the initial position).
    /// </summary>
    public int InitialChunkRadius { get; set; } = 5;
}
