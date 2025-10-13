using System.Numerics;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;
using SquidCraft.Game.Data.Utils;
using SquidCraft.Services.Game.Interfaces;
using Serilog;

namespace SquidCraft.Services.Game.Impl;

/// <summary>
/// Manages world operations including block modifications and chunk access.
/// </summary>
public class WorldManagerService : IWorldManagerService
{
    private readonly ILogger _logger = Log.ForContext<WorldManagerService>();
    private readonly IChunkGeneratorService _chunkGeneratorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldManagerService"/> class.
    /// </summary>
    /// <param name="chunkGeneratorService">The chunk generator service.</param>
    public WorldManagerService(IChunkGeneratorService chunkGeneratorService)
    {
        _chunkGeneratorService = chunkGeneratorService ?? throw new ArgumentNullException(nameof(chunkGeneratorService));
    }

    /// <inheritdoc/>
    public async Task<BlockEntity> GetBlock(Vector3 position)
    {
        _logger.Debug("Getting block at position {Position}", position);

        try
        {
            // Get the chunk containing this position
            var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(position);

            // Get the local position within the chunk
            var (localX, localY, localZ) = ChunkUtils.GetLocalIndices(position);

            // Validate coordinates are within chunk bounds
            if (!ChunkUtils.IsValidLocalPosition(localX, localY, localZ))
            {
                _logger.Error(
                    "Invalid local position: ({X}, {Y}, {Z}) for chunk at world position {WorldPos}",
                    localX,
                    localY,
                    localZ,
                    position
                );
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    $"Position {position} results in invalid local coordinates ({localX}, {localY}, {localZ})"
                );
            }

            // Get the block
            var block = chunk.GetBlock(localX, localY, localZ);

            _logger.Debug(
                "Retrieved block at world position {Position} (local: {LocalX}, {LocalY}, {LocalZ}): {BlockType}",
                position,
                localX,
                localY,
                localZ,
                block.BlockType
            );

            return block;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get block at position {Position}", position);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ModifyBlock(Vector3 position, BlockType blockType)
    {
        _logger.Debug("Modifying block at position {Position} to type {BlockType}", position, blockType);

        try
        {
            // Get the chunk containing this position
            var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(position);

            // Get the local position within the chunk
            var (localX, localY, localZ) = ChunkUtils.GetLocalIndices(position);

            // Validate coordinates are within chunk bounds
            if (!ChunkUtils.IsValidLocalPosition(localX, localY, localZ))
            {
                _logger.Error(
                    "Invalid local position: ({X}, {Y}, {Z}) for chunk at world position {WorldPos}",
                    localX,
                    localY,
                    localZ,
                    position
                );
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    $"Position {position} results in invalid local coordinates ({localX}, {localY}, {localZ})"
                );
            }

            // Get the existing block to preserve its ID
            var existingBlock = chunk.GetBlock(localX, localY, localZ);

            // Create new block with the same ID but different type
            var newBlock = new BlockEntity(existingBlock.Id, blockType);

            // Set the modified block
            chunk.SetBlock(localX, localY, localZ, newBlock);

            _logger.Information(
                "Successfully modified block at world position {Position} (local: {LocalX}, {LocalY}, {LocalZ}) to type {BlockType}",
                position,
                localX,
                localY,
                localZ,
                blockType
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to modify block at position {Position}", position);
            throw;
        }
    }

    public Task RemoveBlock(Vector3 position)
    {
        _logger.Debug("Removing block at world position {Position}", position);
        return ModifyBlock(position, BlockType.Air);
    }

    /// <inheritdoc/>
    public async Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position)
    {
        _logger.Debug("Getting chunk at world position {Position}", position);

        try
        {
            var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(position);
            _logger.Debug("Retrieved chunk at position {Position}", chunk.Position);
            return chunk;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get chunk at position {Position}", position);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChunkEntity>> GetChunksByPositions(IEnumerable<Vector3> positions)
    {
        _logger.Debug("Getting chunks for {Count} positions", positions.Count());

        try
        {
            var chunks = await _chunkGeneratorService.GetChunksByPositions(positions);
            _logger.Information("Retrieved {Count} chunks", chunks.Count());
            return chunks;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get chunks for multiple positions");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChunkEntity>> GetChunksInRadius(Vector3 worldPosition, Vector3 radius)
    {
        _logger.Debug("Getting chunks in radius {Radius} around world position {Position}", radius, worldPosition);

        // Validate that all radius components are non-negative
        if (radius.X < 0 || radius.Y < 0 || radius.Z < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "All radius components must be non-negative");
        }

        try
        {
            // Get the center chunk coordinates
            var centerChunkCoords = ChunkUtils.GetChunkCoordinates(worldPosition);
            var chunks = new List<ChunkEntity>();

            // Convert radius to integers
            int radiusX = (int)radius.X;
            int radiusY = (int)radius.Y;
            int radiusZ = (int)radius.Z;

            // Iterate through all chunks in the radius
            // For radius (1, 0, 1): 3x1x3 chunks
            // For radius (2, 1, 2): 5x3x5 chunks
            for (int dx = -radiusX; dx <= radiusX; dx++)
            {
                for (int dy = -radiusY; dy <= radiusY; dy++)
                {
                    for (int dz = -radiusZ; dz <= radiusZ; dz++)
                    {
                        // Get chunk world position at offset using utility method
                        var chunkWorldPosition = ChunkUtils.GetOffsetChunkWorldPosition(centerChunkCoords, dx, dy, dz);

                        // Get the chunk at this position
                        var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(chunkWorldPosition);
                        chunks.Add(chunk);

                        _logger.Debug(
                            "Retrieved chunk at offset ({OffsetX}, {OffsetY}, {OffsetZ}) - World position {WorldPos}",
                            dx,
                            dy,
                            dz,
                            chunkWorldPosition
                        );
                    }
                }
            }

            _logger.Information(
                "Successfully retrieved {ChunkCount} chunks in radius {Radius} around world position {Position}",
                chunks.Count,
                radius,
                worldPosition
            );

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get chunks in radius {Radius} around position {Position}", radius, worldPosition);
            throw;
        }
    }
}
