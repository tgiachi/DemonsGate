using System.Numerics;
using DemonsGate.Game.Data.Primitives;

namespace DemonsGate.Game.Data.Utils;

/// <summary>
/// Utility methods for chunk operations.
/// </summary>
public static class ChunkUtils
{
    /// <summary>
    /// Normalizes a world position to chunk coordinates.
    /// </summary>
    /// <param name="worldPosition">The world position to normalize.</param>
    /// <returns>The normalized chunk position.</returns>
    public static Vector3 NormalizeToChunkPosition(Vector3 worldPosition)
    {
        // Calculate chunk coordinates by dividing world position by chunk size
        int chunkX = (int)Math.Floor(worldPosition.X / ChunkEntity.Size) * ChunkEntity.Size;
        int chunkY = (int)Math.Floor(worldPosition.Y / ChunkEntity.Height) * ChunkEntity.Height;
        int chunkZ = (int)Math.Floor(worldPosition.Z / ChunkEntity.Size) * ChunkEntity.Size;

        return new Vector3(chunkX, chunkY, chunkZ);
    }

    /// <summary>
    /// Gets the chunk position for a given world position.
    /// </summary>
    /// <param name="worldPosition">The world position.</param>
    /// <returns>The chunk coordinates (not multiplied by size).</returns>
    public static Vector3 GetChunkCoordinates(Vector3 worldPosition)
    {
        int chunkX = (int)Math.Floor(worldPosition.X / ChunkEntity.Size);
        int chunkY = (int)Math.Floor(worldPosition.Y / ChunkEntity.Height);
        int chunkZ = (int)Math.Floor(worldPosition.Z / ChunkEntity.Size);

        return new Vector3(chunkX, chunkY, chunkZ);
    }

    /// <summary>
    /// Converts chunk coordinates to world position.
    /// </summary>
    /// <param name="chunkX">Chunk X coordinate.</param>
    /// <param name="chunkY">Chunk Y coordinate.</param>
    /// <param name="chunkZ">Chunk Z coordinate.</param>
    /// <returns>The world position of the chunk.</returns>
    public static Vector3 ChunkCoordinatesToWorldPosition(int chunkX, int chunkY, int chunkZ)
    {
        return new Vector3(
            chunkX * ChunkEntity.Size,
            chunkY * ChunkEntity.Height,
            chunkZ * ChunkEntity.Size
        );
    }

    /// <summary>
    /// Gets the local position within a chunk from a world position.
    /// </summary>
    /// <param name="worldPosition">The world position.</param>
    /// <returns>The local position within the chunk (0 to Size-1 for X/Z, 0 to Height-1 for Y).</returns>
    public static Vector3 GetLocalPosition(Vector3 worldPosition)
    {
        float localX = worldPosition.X % ChunkEntity.Size;
        float localY = worldPosition.Y % ChunkEntity.Height;
        float localZ = worldPosition.Z % ChunkEntity.Size;

        // Handle negative positions
        if (localX < 0) localX += ChunkEntity.Size;
        if (localY < 0) localY += ChunkEntity.Height;
        if (localZ < 0) localZ += ChunkEntity.Size;

        return new Vector3(localX, localY, localZ);
    }

    /// <summary>
    /// Checks if a world position is within chunk bounds.
    /// </summary>
    /// <param name="worldPosition">The world position to check.</param>
    /// <param name="chunkPosition">The chunk position.</param>
    /// <returns>True if the position is within the chunk bounds; otherwise, false.</returns>
    public static bool IsPositionInChunk(Vector3 worldPosition, Vector3 chunkPosition)
    {
        return worldPosition.X >= chunkPosition.X && worldPosition.X < chunkPosition.X + ChunkEntity.Size &&
               worldPosition.Y >= chunkPosition.Y && worldPosition.Y < chunkPosition.Y + ChunkEntity.Height &&
               worldPosition.Z >= chunkPosition.Z && worldPosition.Z < chunkPosition.Z + ChunkEntity.Size;
    }
}
