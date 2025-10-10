using System.Numerics;
using DemonsGate.Core.Interfaces.Metrics;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Game.Data.Utils;
using DemonsGate.Services.Game.Data.Config;
using DemonsGate.Services.Game.Impl;
using DemonsGate.Services.Game.Impl.Pipeline.Steps;
using DemonsGate.Services.Game.Metrics;
using DemonsGate.Services.Game.Types;
using DemonsGate.Services.Interfaces;
using NSubstitute;

namespace DemonsGate.Tests.Services.Game;

[TestFixture]
/// <summary>
/// Contains integration test cases for ChunkGeneratorService.
/// </summary>
public class ChunkGeneratorServiceIntegrationTests
{
    private ITimerService _mockTimerService = null!;
    private IDiagnosticService _mockDiagnosticService = null!;
    private ChunkGeneratorService _chunkGeneratorService = null!;
    private ChunkGeneratorConfig _config = null!;

    [SetUp]
    public void SetUp()
    {
        _mockTimerService = Substitute.For<ITimerService>();
        _mockDiagnosticService = Substitute.For<IDiagnosticService>();

        _config = new ChunkGeneratorConfig
        {
            Seed = 12345,
            CacheExpirationMinutes = 5,
            InitialPosition = Vector3.Zero,
            InitialChunkRadius = 2
        };

        _chunkGeneratorService = new ChunkGeneratorService(
            _mockTimerService,
            _mockDiagnosticService,
            _config
        );
    }

    [TearDown]
    public void TearDown()
    {
        _chunkGeneratorService?.Dispose();
    }

    [Test]
    public async Task GetChunkByWorldPosition_ShouldGenerateChunk()
    {
        // Arrange
        var worldPosition = new Vector3(0, 0, 0);

        // Act
        var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(worldPosition);

        // Assert
        Assert.That(chunk, Is.Not.Null);
        Assert.That(chunk.Position, Is.EqualTo(ChunkUtils.NormalizeToChunkPosition(worldPosition)));
    }

    [Test]
    public async Task GetChunkByWorldPosition_ShouldCacheChunk()
    {
        // Arrange
        var worldPosition = new Vector3(0, 0, 0);

        // Act
        var chunk1 = await _chunkGeneratorService.GetChunkByWorldPosition(worldPosition);
        var chunk2 = await _chunkGeneratorService.GetChunkByWorldPosition(worldPosition);

        // Assert
        Assert.That(chunk1, Is.SameAs(chunk2), "Should return the same cached chunk instance");
    }

    [Test]
    public async Task GetChunkByWorldPosition_ShouldGenerateTerrainWithBlocks()
    {
        // Arrange
        var worldPosition = new Vector3(0, 0, 0);

        // Act
        var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(worldPosition);

        // Assert - Check that the chunk has terrain generated
        var hasNonAirBlocks = false;
        for (int x = 0; x < ChunkEntity.Size && !hasNonAirBlocks; x++)
        {
            for (int y = 0; y < ChunkEntity.Height && !hasNonAirBlocks; y++)
            {
                for (int z = 0; z < ChunkEntity.Size && !hasNonAirBlocks; z++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    if (block.BlockType != BlockType.Air)
                    {
                        hasNonAirBlocks = true;
                    }
                }
            }
        }

        Assert.That(hasNonAirBlocks, Is.True, "Chunk should contain non-air blocks");
    }

    [Test]
    public async Task GetChunkByWorldPosition_ShouldNormalizePosition()
    {
        // Arrange
        var position1 = new Vector3(5, 5, 5);
        var position2 = new Vector3(10, 10, 10);
        var normalizedPos = ChunkUtils.NormalizeToChunkPosition(position1);

        // Act
        var chunk1 = await _chunkGeneratorService.GetChunkByWorldPosition(position1);
        var chunk2 = await _chunkGeneratorService.GetChunkByWorldPosition(position2);

        // Assert
        Assert.That(chunk1.Position, Is.EqualTo(normalizedPos));
        Assert.That(chunk1, Is.SameAs(chunk2), "Both positions should resolve to the same chunk");
    }

    [Test]
    public async Task StartAsync_ShouldGenerateInitialChunks()
    {
        // Arrange
        var expectedChunkCount = ((_config.InitialChunkRadius * 2) + 1) * ((_config.InitialChunkRadius * 2) + 1);

        // Act
        await _chunkGeneratorService.StartAsync();

        // Assert
        Assert.That(_chunkGeneratorService.CachedChunkCount, Is.EqualTo(expectedChunkCount),
            "Should have generated initial chunks based on radius");
    }

    [Test]
    public async Task AddGeneratorStep_ShouldIncludeStepInPipeline()
    {
        // Arrange
        var customStep = new TerrainGeneratorStep();

        // Act
        _chunkGeneratorService.AddGeneratorStep(customStep);
        var steps = _chunkGeneratorService.GetGeneratorSteps();

        // Assert
        Assert.That(steps.Count, Is.EqualTo(2), "Should have 2 steps (1 default + 1 added)");
    }

    [Test]
    public void RemoveGeneratorStep_ShouldRemoveStepFromPipeline()
    {
        // Arrange
        var stepName = "TerrainGenerator";

        // Act
        var removed = _chunkGeneratorService.RemoveGeneratorStep(stepName);
        var steps = _chunkGeneratorService.GetGeneratorSteps();

        // Assert
        Assert.That(removed, Is.True, "Should successfully remove the step");
        Assert.That(steps.Count, Is.EqualTo(0), "Pipeline should be empty after removing step");
    }

    [Test]
    public void ClearCache_ShouldRemoveAllCachedChunks()
    {
        // Arrange
        var worldPosition = new Vector3(0, 0, 0);
        _chunkGeneratorService.GetChunkByWorldPosition(worldPosition).Wait();
        var initialCount = _chunkGeneratorService.CachedChunkCount;

        // Act
        _chunkGeneratorService.ClearCache();

        // Assert
        Assert.That(initialCount, Is.GreaterThan(0), "Should have chunks before clearing");
        Assert.That(_chunkGeneratorService.CachedChunkCount, Is.EqualTo(0), "Cache should be empty after clearing");
    }

    [Test]
    public void GetMetrics_ShouldReturnChunkGeneratorMetrics()
    {
        // Arrange
        var worldPosition = new Vector3(0, 0, 0);
        _chunkGeneratorService.GetChunkByWorldPosition(worldPosition).Wait();

        // Act
        var metrics = _chunkGeneratorService.GetMetrics() as ChunkGeneratorMetrics;

        // Assert
        Assert.That(metrics, Is.Not.Null);
        Assert.That(metrics!.Seed, Is.EqualTo(_config.Seed));
        Assert.That(metrics.CacheExpirationMinutes, Is.EqualTo(_config.CacheExpirationMinutes));
        Assert.That(metrics.TotalChunksGenerated, Is.GreaterThan(0));
    }

    [Test]
    public void GetMetrics_ShouldTrackCacheHitRate()
    {
        // Arrange
        var worldPosition = new Vector3(0, 0, 0);
        _chunkGeneratorService.GetChunkByWorldPosition(worldPosition).Wait();
        _chunkGeneratorService.GetChunkByWorldPosition(worldPosition).Wait(); // Hit

        // Act
        var metrics = _chunkGeneratorService.GetMetrics() as ChunkGeneratorMetrics;

        // Assert
        Assert.That(metrics, Is.Not.Null);
        Assert.That(metrics!.CacheHits, Is.EqualTo(1));
        Assert.That(metrics.CacheMisses, Is.EqualTo(1));
        Assert.That(metrics.CacheHitRate, Is.EqualTo(50.0));
    }

    [Test]
    public async Task GetChunkByWorldPosition_ShouldGenerateDifferentChunksForDifferentPositions()
    {
        // Arrange
        var position1 = new Vector3(0, 0, 0);
        var position2 = new Vector3(100, 0, 100);

        // Act
        var chunk1 = await _chunkGeneratorService.GetChunkByWorldPosition(position1);
        var chunk2 = await _chunkGeneratorService.GetChunkByWorldPosition(position2);

        // Assert
        Assert.That(chunk1, Is.Not.SameAs(chunk2), "Different positions should generate different chunks");
        Assert.That(chunk1.Position, Is.Not.EqualTo(chunk2.Position), "Chunks should have different positions");
    }

    [Test]
    public void ProviderName_ShouldReturnCorrectName()
    {
        // Act
        var providerName = _chunkGeneratorService.ProviderName;

        // Assert
        Assert.That(providerName, Is.EqualTo("ChunkGenerator"));
    }

    [Test]
    public async Task StopAsync_ShouldClearCache()
    {
        // Arrange
        var worldPosition = new Vector3(0, 0, 0);
        await _chunkGeneratorService.GetChunkByWorldPosition(worldPosition);
        var initialCount = _chunkGeneratorService.CachedChunkCount;

        // Act
        await _chunkGeneratorService.StopAsync();

        // Assert
        Assert.That(initialCount, Is.GreaterThan(0), "Should have chunks before stopping");
        Assert.That(_chunkGeneratorService.CachedChunkCount, Is.EqualTo(0), "Cache should be empty after stopping");
    }
}
