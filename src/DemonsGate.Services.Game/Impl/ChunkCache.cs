using System.Collections.Concurrent;
using System.Numerics;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Services.Interfaces;
using Serilog;

namespace DemonsGate.Services.Game.Impl;

/// <summary>
/// Manages a time-based cache for active chunks to avoid regenerating frequently accessed chunks.
/// </summary>
public class ChunkCache
{
    private readonly ILogger _logger = Log.ForContext<ChunkCache>();
    private readonly ConcurrentDictionary<Vector3, CacheEntry> _cache = new();
    private readonly TimeSpan _expirationTime;
    private readonly ITimerService _timerService;
    private string? _cleanupTimerId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkCache"/> class.
    /// </summary>
    /// <param name="timerService">Timer service for scheduling cache cleanup.</param>
    /// <param name="expirationTime">Time after which inactive chunks are removed from cache.</param>
    public ChunkCache(ITimerService timerService, TimeSpan expirationTime)
    {
        _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
        _expirationTime = expirationTime;

        // Register cleanup timer to run every minute
        _cleanupTimerId = _timerService.RegisterTimer(
            "ChunkCacheCleanup",
            intervalInMs: 60000, // 60 seconds
            callback: CleanupExpiredEntries,
            delayInMs: 60000,
            repeat: true
        );

        _logger.Information("ChunkCache initialized with cleanup timer running every minute");
    }

    /// <summary>
    /// Tries to get a chunk from the cache.
    /// </summary>
    /// <param name="position">The world position of the chunk.</param>
    /// <param name="chunk">The cached chunk, if found.</param>
    /// <returns>True if the chunk was found in cache; otherwise, false.</returns>
    public bool TryGet(Vector3 position, out ChunkEntity? chunk)
    {
        if (_cache.TryGetValue(position, out var entry))
        {
            // Update last access time
            entry.LastAccessTime = DateTime.UtcNow;
            chunk = entry.Chunk;

            _logger.Debug("Cache hit for chunk at {Position}", position);
            return true;
        }

        chunk = null;
        _logger.Debug("Cache miss for chunk at {Position}", position);
        return false;
    }

    /// <summary>
    /// Adds or updates a chunk in the cache.
    /// </summary>
    /// <param name="position">The world position of the chunk.</param>
    /// <param name="chunk">The chunk to cache.</param>
    public void Set(Vector3 position, ChunkEntity chunk)
    {
        var entry = new CacheEntry
        {
            Chunk = chunk,
            LastAccessTime = DateTime.UtcNow
        };

        _cache.AddOrUpdate(position, entry, (_, _) => entry);
        _logger.Debug("Cached chunk at {Position}. Total cached chunks: {Count}", position, _cache.Count);
    }

    /// <summary>
    /// Removes a chunk from the cache.
    /// </summary>
    /// <param name="position">The world position of the chunk to remove.</param>
    /// <returns>True if the chunk was removed; otherwise, false.</returns>
    public bool Remove(Vector3 position)
    {
        bool removed = _cache.TryRemove(position, out _);
        if (removed)
        {
            _logger.Debug("Removed chunk at {Position} from cache", position);
        }
        return removed;
    }

    /// <summary>
    /// Gets the number of chunks currently in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        int count = _cache.Count;
        _cache.Clear();
        _logger.Information("Cleared {Count} chunks from cache", count);
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    private void CleanupExpiredEntries()
    {
        var now = DateTime.UtcNow;
        var expiredEntries = _cache
            .Where(kvp => now - kvp.Value.LastAccessTime > _expirationTime)
            .Select(kvp => kvp.Key)
            .ToList();

        if (expiredEntries.Count > 0)
        {
            _logger.Debug("Cleaning up {Count} expired chunks from cache", expiredEntries.Count);

            foreach (var position in expiredEntries)
            {
                _cache.TryRemove(position, out _);
            }

            _logger.Information("Removed {Count} expired chunks. Remaining: {Remaining}",
                expiredEntries.Count, _cache.Count);
        }
    }

    /// <summary>
    /// Represents a cached chunk with its access metadata.
    /// </summary>
    private class CacheEntry
    {
        public required ChunkEntity Chunk { get; init; }
        public DateTime LastAccessTime { get; set; }
    }

    /// <summary>
    /// Disposes resources used by the cache.
    /// </summary>
    public void Dispose()
    {
        if (_cleanupTimerId != null)
        {
            _timerService.UnregisterTimer(_cleanupTimerId);
            _cleanupTimerId = null;
        }
        Clear();
    }
}
