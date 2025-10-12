using System.Numerics;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Services.Game.Interfaces;

public interface IWorldManagerService
{
    /// <summary>
    /// Gets a block at the specified world position.
    /// </summary>
    /// <param name="position">The world position of the block.</param>
    /// <returns>The block at the specified position.</returns>
    Task<BlockEntity> GetBlock(Vector3 position);

    /// <summary>
    /// Modifies a block at the specified world position.
    /// </summary>
    /// <param name="position">The world position of the block.</param>
    /// <param name="blockType">The new block type.</param>
    Task ModifyBlock(Vector3 position, BlockType blockType);

    /// <summary>
    ///  Remove block
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    Task RemoveBlock(Vector3 position);

    Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position);

    /// <summary>
    /// Gets all chunks within a specified radius from the given world position.
    /// </summary>
    /// <param name="worldPosition">The center world position.</param>
    /// <param name="radius">The radius in chunks for each axis (e.g., Vector3(1, 0, 1) returns 3x1x3 chunks, Vector3(2, 1, 2) returns 5x3x5 chunks).</param>
    /// <returns>A collection of chunks within the specified radius.</returns>
    Task<IEnumerable<ChunkEntity>> GetChunksInRadius(Vector3 worldPosition, Vector3 radius);
}
