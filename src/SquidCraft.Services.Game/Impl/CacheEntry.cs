using System;
using SquidCraft.Game.Data.Primitives;

namespace SquidCraft.Services.Game.Impl;

/// <summary>
/// Represents a cached chunk with its access metadata.
/// </summary>
internal class CacheEntry
{
    public required ChunkEntity Chunk { get; init; }
    public DateTime LastAccessTime { get; set; }
}