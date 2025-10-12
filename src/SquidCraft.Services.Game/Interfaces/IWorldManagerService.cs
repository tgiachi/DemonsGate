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
}
