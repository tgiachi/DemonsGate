using System.Numerics;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Services.Game.Generation.Noise;
using DemonsGate.Services.Game.Impl.Pipeline;
using DemonsGate.Services.Game.Impl.Pipeline.Steps;
using DemonsGate.Services.Game.Interfaces;
using DemonsGate.Services.Game.Interfaces.Pipeline;
using DemonsGate.Services.Interfaces;
using Serilog;

namespace DemonsGate.Services.Game.Impl;

/// <summary>
/// Manages chunk generation using a configurable pipeline and time-based cache.
/// </summary>
public class ChunkGeneratorService : IChunkGeneratorService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<ChunkGeneratorService>();
    private readonly ChunkCache _chunkCache;
    private readonly List<IGeneratorStep> _pipeline;
    private readonly FastNoiseLite _noiseGenerator;
    private readonly int _seed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkGeneratorService"/> class.
    /// </summary>
    /// <param name="timerService">Timer service for cache management.</param>
    /// <param name="seed">Seed for procedural generation. If not provided, a random seed is used.</param>
    /// <param name="cacheExpirationMinutes">Cache expiration time in minutes. Default is 5 minutes.</param>
    public ChunkGeneratorService(ITimerService timerService, int? seed = null, int cacheExpirationMinutes = 5)
    {
        ArgumentNullException.ThrowIfNull(timerService);

        _seed = seed ?? Random.Shared.Next();
        _logger.Information("Initializing ChunkGeneratorService with seed: {Seed}", _seed);

        // Initialize noise generator
        _noiseGenerator = new FastNoiseLite(_seed);
        _noiseGenerator.SetNoiseType(NoiseType.OpenSimplex2);
        _noiseGenerator.SetFrequency(0.01f);

        // Initialize cache with expiration time
        _chunkCache = new ChunkCache(timerService, TimeSpan.FromMinutes(cacheExpirationMinutes));
        _logger.Information("Chunk cache initialized with {Minutes} minute expiration", cacheExpirationMinutes);

        // Initialize generation pipeline
        _pipeline = new List<IGeneratorStep>
        {
            new TerrainGeneratorStep()
            // Future steps can be added here:
            // new CaveGeneratorStep(),
            // new OreGeneratorStep(),
            // new BiomeDecorationStep(),
            // etc.
        };

        _logger.Information("Generation pipeline initialized with {StepCount} steps: {Steps}",
            _pipeline.Count, string.Join(", ", _pipeline.Select(s => s.Name)));
    }

    /// <inheritdoc/>
    public async Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position)
    {
        _logger.Debug("Requested chunk at world position {Position}", position);

        // Normalize position to chunk coordinates
        var chunkPosition = NormalizeToChunkPosition(position);

        // Try to get from cache first
        if (_chunkCache.TryGet(chunkPosition, out var cachedChunk) && cachedChunk != null)
        {
            _logger.Debug("Returning cached chunk at {Position}", chunkPosition);
            return cachedChunk;
        }

        // Generate new chunk
        _logger.Information("Generating new chunk at {Position}", chunkPosition);
        var chunk = await GenerateChunkAsync(chunkPosition);

        // Cache the generated chunk
        _chunkCache.Set(chunkPosition, chunk);

        return chunk;
    }

    /// <summary>
    /// Generates a new chunk at the specified position using the generation pipeline.
    /// </summary>
    /// <param name="chunkPosition">The normalized chunk position.</param>
    /// <returns>The generated chunk.</returns>
    private async Task<ChunkEntity> GenerateChunkAsync(Vector3 chunkPosition)
    {
        var chunk = new ChunkEntity(chunkPosition);
        var context = new GeneratorContext(chunk, chunkPosition, _noiseGenerator, _seed);

        _logger.Debug("Starting generation pipeline for chunk at {Position}", chunkPosition);

        // Execute each step in the pipeline
        foreach (var step in _pipeline)
        {
            _logger.Debug("Executing generation step: {StepName}", step.Name);
            await step.ExecuteAsync(context);
        }

        _logger.Information("Chunk generation completed at {Position}", chunkPosition);
        return chunk;
    }

    /// <summary>
    /// Normalizes a world position to chunk coordinates.
    /// </summary>
    /// <param name="worldPosition">The world position to normalize.</param>
    /// <returns>The normalized chunk position.</returns>
    private static Vector3 NormalizeToChunkPosition(Vector3 worldPosition)
    {
        // Calculate chunk coordinates by dividing world position by chunk size
        int chunkX = (int)Math.Floor(worldPosition.X / ChunkEntity.Size) * ChunkEntity.Size;
        int chunkY = (int)Math.Floor(worldPosition.Y / ChunkEntity.Height) * ChunkEntity.Height;
        int chunkZ = (int)Math.Floor(worldPosition.Z / ChunkEntity.Size) * ChunkEntity.Size;

        return new Vector3(chunkX, chunkY, chunkZ);
    }

    /// <summary>
    /// Gets the current number of cached chunks.
    /// </summary>
    public int CachedChunkCount => _chunkCache.Count;

    /// <summary>
    /// Clears all cached chunks.
    /// </summary>
    public void ClearCache()
    {
        _logger.Information("Clearing chunk cache");
        _chunkCache.Clear();
    }

    /// <summary>
    /// Adds a generation step to the pipeline.
    /// </summary>
    /// <param name="step">The generator step to add.</param>
    public void AddGeneratorStep(IGeneratorStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _pipeline.Add(step);
        _logger.Information("Added generator step '{StepName}' to pipeline. Total steps: {Count}", step.Name, _pipeline.Count);
    }

    /// <summary>
    /// Removes a generation step from the pipeline by name.
    /// </summary>
    /// <param name="stepName">The name of the step to remove.</param>
    /// <returns>True if the step was removed; otherwise, false.</returns>
    public bool RemoveGeneratorStep(string stepName)
    {
        var step = _pipeline.FirstOrDefault(s => s.Name == stepName);
        if (step != null)
        {
            _pipeline.Remove(step);
            _logger.Information("Removed generator step '{StepName}' from pipeline. Remaining steps: {Count}", stepName, _pipeline.Count);
            return true;
        }

        _logger.Warning("Generator step '{StepName}' not found in pipeline", stepName);
        return false;
    }

    /// <summary>
    /// Gets all generator steps in the pipeline.
    /// </summary>
    public IReadOnlyList<IGeneratorStep> GetGeneratorSteps() => _pipeline.AsReadOnly();

    /// <summary>
    /// Clears all generator steps from the pipeline.
    /// </summary>
    public void ClearGeneratorSteps()
    {
        _pipeline.Clear();
        _logger.Information("Cleared all generator steps from pipeline");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _logger.Information("Disposing ChunkGeneratorService");
        _chunkCache.Dispose();
    }
}
