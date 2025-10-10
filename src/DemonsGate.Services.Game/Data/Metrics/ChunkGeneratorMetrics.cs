namespace DemonsGate.Services.Game.Metrics;

/// <summary>
/// Metrics data for the chunk generator service.
/// </summary>
public class ChunkGeneratorMetrics
{
    /// <summary>
    /// Gets or sets the number of chunks currently in cache.
    /// </summary>
    public int CachedChunkCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of chunks generated since service start.
    /// </summary>
    public long TotalChunksGenerated { get; set; }

    /// <summary>
    /// Gets or sets the number of cache hits.
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Gets or sets the number of cache misses.
    /// </summary>
    public long CacheMisses { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate as a percentage.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the number of steps in the generation pipeline.
    /// </summary>
    public int PipelineStepCount { get; set; }

    /// <summary>
    /// Gets or sets the generation seed.
    /// </summary>
    public int Seed { get; set; }

    /// <summary>
    /// Gets or sets the cache expiration time in minutes.
    /// </summary>
    public int CacheExpirationMinutes { get; set; }

    /// <summary>
    /// Gets or sets the initial chunk radius.
    /// </summary>
    public int InitialChunkRadius { get; set; }
}
