using System.Diagnostics;
using System.Numerics;
using DemonsGate.Core.Interfaces.Metrics;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Game.Data.Utils;
using DemonsGate.Services.Game.Data.Config;
using DemonsGate.Services.Game.Generation.Noise;
using DemonsGate.Services.Game.Impl.Pipeline;
using DemonsGate.Services.Game.Impl.Pipeline.Steps;
using DemonsGate.Services.Game.Interfaces;
using DemonsGate.Services.Game.Interfaces.Pipeline;
using DemonsGate.Services.Game.Metrics;
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
    private readonly ChunkGeneratorConfig _config;
    private readonly int _seed;

    // Metrics counters
    private long _totalChunksGenerated;
    private long _cacheHits;
    private long _cacheMisses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkGeneratorService"/> class.
    /// </summary>
    /// <param name="timerService">Timer service for cache management.</param>
    /// <param name="config">Chunk generator configuration.</param>
    public ChunkGeneratorService(
        ITimerService timerService, IDiagnosticService diagnosticService, ChunkGeneratorConfig config
    )
    {
        ArgumentNullException.ThrowIfNull(timerService);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(diagnosticService);

        diagnosticService.RegisterMetricsProvider(this);
        _config = config;
        _seed = config.Seed ?? Random.Shared.Next();
        _logger.Information("Initializing ChunkGeneratorService with seed: {Seed}", _seed);

        // Initialize noise generator
        _noiseGenerator = new FastNoiseLite(_seed);
        _noiseGenerator.SetNoiseType(NoiseType.OpenSimplex2);
        _noiseGenerator.SetFrequency(0.01f);

        // Initialize cache with expiration time
        _chunkCache = new ChunkCache(timerService, TimeSpan.FromMinutes(config.CacheExpirationMinutes));
        _logger.Information("Chunk cache initialized with {Minutes} minute expiration", config.CacheExpirationMinutes);

        // Initialize generation pipeline
        _pipeline = [new TerrainGeneratorStep()];

        _logger.Information(
            "Generation pipeline initialized with {StepCount} steps: {Steps}",
            _pipeline.Count,
            string.Join(", ", _pipeline.Select(s => s.Name))
        );
    }

    /// <inheritdoc/>
    public async Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position)
    {
        _logger.Debug("Requested chunk at world position {Position}", position);

        // Normalize position to chunk coordinates
        var chunkPosition = ChunkUtils.NormalizeToChunkPosition(position);

        // Try to get from cache first
        if (_chunkCache.TryGet(chunkPosition, out var cachedChunk) && cachedChunk != null)
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.Debug("Returning cached chunk at {Position}", chunkPosition);
            return cachedChunk;
        }

        // Cache miss
        Interlocked.Increment(ref _cacheMisses);

        // Generate new chunk
        _logger.Information("Generating new chunk at {Position}", chunkPosition);
        var chunk = await GenerateChunkAsync(chunkPosition);

        // Cache the generated chunk
        _chunkCache.Set(chunkPosition, chunk);

        return chunk;
    }

    public async Task GenerateInitialChunksAsync()
    {
        var startTime = Stopwatch.GetTimestamp();
        _logger.Information(
            "Generating initial chunks with radius {Radius} around position {Position}",
            _config.InitialChunkRadius,
            _config.InitialPosition
        );

        var chunksToGenerate = new List<Vector3>();

        // Normalize the initial position to chunk coordinates
        var centerChunkPos = ChunkUtils.NormalizeToChunkPosition(_config.InitialPosition);

        // Calculate all chunk positions to generate in a radius around the initial position
        for (int x = -_config.InitialChunkRadius; x <= _config.InitialChunkRadius; x++)
        {
            for (int z = -_config.InitialChunkRadius; z <= _config.InitialChunkRadius; z++)
            {
                var chunkPos = new Vector3(
                    centerChunkPos.X + (x * ChunkEntity.Size),
                    centerChunkPos.Y,
                    centerChunkPos.Z + (z * ChunkEntity.Size)
                );
                chunksToGenerate.Add(chunkPos);
            }
        }

        _logger.Information("Generating {Count} initial chunks", chunksToGenerate.Count);

        // Generate chunks in parallel
        var tasks = chunksToGenerate.Select(GetChunkByWorldPosition);
        await Task.WhenAll(tasks);

        _logger.Information("Initial chunk generation completed. Generated {Count} chunks", chunksToGenerate.Count);
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

        Interlocked.Increment(ref _totalChunksGenerated);
        _logger.Debug("Chunk generation completed at {Position}", chunkPosition);
        return chunk;
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
        _logger.Information(
            "Added generator step '{StepName}' to pipeline. Total steps: {Count}",
            step.Name,
            _pipeline.Count
        );
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
            _logger.Information(
                "Removed generator step '{StepName}' from pipeline. Remaining steps: {Count}",
                stepName,
                _pipeline.Count
            );
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
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting ChunkGeneratorService");

        try
        {
            await GenerateInitialChunksAsync();
            _logger.Information("ChunkGeneratorService started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start ChunkGeneratorService");
            throw;
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Stopping ChunkGeneratorService");
        ClearCache();
        _logger.Information("ChunkGeneratorService stopped");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public string ProviderName => "ChunkGenerator";

    /// <inheritdoc/>
    public object GetMetrics()
    {
        long totalRequests = _cacheHits + _cacheMisses;
        double cacheHitRate = totalRequests > 0 ? (_cacheHits / (double)totalRequests) * 100.0 : 0.0;

        return new ChunkGeneratorMetrics
        {
            CachedChunkCount = _chunkCache.Count,
            TotalChunksGenerated = _totalChunksGenerated,
            CacheHits = _cacheHits,
            CacheMisses = _cacheMisses,
            CacheHitRate = cacheHitRate,
            PipelineStepCount = _pipeline.Count,
            Seed = _seed,
            CacheExpirationMinutes = _config.CacheExpirationMinutes,
            InitialChunkRadius = _config.InitialChunkRadius
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _logger.Information("Disposing ChunkGeneratorService");
        _chunkCache.Dispose();

        GC.SuppressFinalize(this);
    }
}
