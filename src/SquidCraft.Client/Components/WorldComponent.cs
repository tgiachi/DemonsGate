using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using SquidCraft.Game.Data.Primitives;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using SysVector3 = System.Numerics.Vector3;

namespace SquidCraft.Client.Components;

public sealed class WorldComponent : IDisposable
{
    private readonly ILogger _logger = Log.ForContext<WorldComponent>();
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ConcurrentDictionary<SysVector3, ChunkComponent> _chunks = new();
    private readonly ConcurrentQueue<(SysVector3 Position, ChunkEntity Chunk)> _pendingChunks = new();
    private readonly Queue<ChunkComponent> _meshBuildQueue = new();

    private readonly CameraComponent _camera;
    private readonly Particle3dComponent _particleComponent;
    private readonly Systems.ChunkLightSystem _lightSystem;
    private readonly Systems.WaterSimulationSystem _waterSystem;
    // private readonly DayNightCycle _dayNightCycle;
    private bool _isDisposed;
    private BoundingFrustum? _frustum;
    private (int X, int Z)? _lastPlayerChunk;
    // private Color _lastSunColor;

    public WorldComponent(GraphicsDevice graphicsDevice, CameraComponent camera)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _particleComponent = new Particle3dComponent();
        _lightSystem = new Systems.ChunkLightSystem();
        _waterSystem = new Systems.WaterSimulationSystem();
        // _dayNightCycle = new DayNightCycle();
        // _lastSunColor = _dayNightCycle.GetSunColor(); // Initialize with current sun color
    }

    public delegate ChunkEntity ChunkGeneratorDelegate(int chunkX, int chunkZ);
    
    public delegate Task<ChunkEntity> ChunkGeneratorAsyncDelegate(int chunkX, int chunkZ);
    
    public ChunkGeneratorDelegate? ChunkGenerator { get; set; }
    
    public ChunkGeneratorAsyncDelegate? ChunkGeneratorAsync { get; set; }

    public CameraComponent Camera => _camera;

    public IReadOnlyDictionary<SysVector3, ChunkComponent> Chunks => _chunks;

    public float ViewRange { get; set; } = 200f;

    public float GenerationRange { get; set; } = 250f;

    public bool EnableFrustumCulling { get; set; } = true;

    public float MaxRaycastDistance { get; set; } = 10f;

    public int ChunkLoadDistance { get; set; } = 2;

    public int GenerationDistance { get; set; } = 3;

    public int MaxChunkBuildsPerFrame { get; set; } = 2;

    public (ChunkComponent? Chunk, int X, int Y, int Z)? SelectedBlock { get; private set; }

    // public DayNightCycle DayNightCycle => _dayNightCycle;

    public async Task AddChunkAsync(ChunkEntity chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        var chunkPosition = chunk.Position;

        await Task.Run(() =>
            {
                _pendingChunks.Enqueue((chunkPosition, chunk));
                _logger.Debug("Chunk queued for addition at position {Position}", chunkPosition);
            }
        );
    }

    public bool RemoveChunk(SysVector3 position)
    {
        if (_chunks.TryRemove(position, out var chunkComponent))
        {
            chunkComponent.Dispose();
            _logger.Debug("Chunk removed at position {Position}", position);
            return true;
        }

        return false;
    }

    public ChunkComponent? GetChunk(SysVector3 position)
    {
        return _chunks.TryGetValue(position, out var chunk) ? chunk : null;
    }

    public ChunkEntity? GetChunkEntity(XnaVector3 position)
    {
        var sysPos = new SysVector3(position.X, position.Y, position.Z);
        var chunk = GetChunk(sysPos);
        return chunk?.Chunk;
    }

    public bool IsBlockSolid(XnaVector3 worldPosition)
    {
        return IsBlockSolidForCollision(worldPosition, false);
    }

    public bool IsBlockSolidForCollision(XnaVector3 worldPosition, bool includeWater = false)
    {
        var blockX = (int)MathF.Floor(worldPosition.X);
        var blockY = (int)MathF.Floor(worldPosition.Y);
        var blockZ = (int)MathF.Floor(worldPosition.Z);

        var chunkX = MathF.Floor(blockX / (float)ChunkEntity.Size) * ChunkEntity.Size;
        var chunkZ = MathF.Floor(blockZ / (float)ChunkEntity.Size) * ChunkEntity.Size;
        var chunkPos = new SysVector3(chunkX, 0f, chunkZ);

        var chunkEntity = GetChunkEntity(new XnaVector3(chunkX, 0f, chunkZ));
        if (chunkEntity == null)
        {
            return false;
        }

        var localX = blockX - (int)chunkEntity.Position.X;
        var localY = blockY - (int)chunkEntity.Position.Y;
        var localZ = blockZ - (int)chunkEntity.Position.Z;

        if (!chunkEntity.IsInBounds(localX, localY, localZ))
        {
            return false;
        }

        var block = chunkEntity.GetBlock(localX, localY, localZ);
        if (block == null || block.BlockType == Game.Data.Types.BlockType.Air)
        {
            return false;
        }

        // L'acqua non è solida per le collisioni (puoi camminarci attraverso)
        if (!includeWater && block.BlockType == Game.Data.Types.BlockType.Water)
        {
            return false;
        }

        return true;
    }

    public void ClearChunks()
    {
        foreach (var chunk in _chunks.Values)
        {
            chunk.Dispose();
        }

        _chunks.Clear();
        _logger.Information("All chunks cleared");
    }

    public void Update(GameTime gameTime)
    {
        ProcessPendingChunks();

        ProcessMeshBuildQueue();

        _camera.Update(gameTime);

        // Update day/night cycle
        // _dayNightCycle.Update(gameTime);

        // Check if sun color changed significantly
        // var currentSunColor = _dayNightCycle.GetSunColor();
        // var colorDifference = Math.Abs(currentSunColor.R - _lastSunColor.R) +
        //                      Math.Abs(currentSunColor.G - _lastSunColor.G) +
        //                      Math.Abs(currentSunColor.B - _lastSunColor.B);

        // if (colorDifference > 0.01f) // If color changed by more than 1%
        // {
        //     _lastSunColor = currentSunColor;

        //     // Invalidate all chunk meshes for lighting update
        //     foreach (var chunk in _chunks.Values)
        //     {
        //         if (chunk.HasMesh)
        //         {
        //             chunk.InvalidateGeometry();
        //             _meshBuildQueue.Enqueue(chunk);
        //         }
        //     }

        //     _logger.Debug("Invalidated all chunk meshes for sun color change (diff: {Difference:F3})", colorDifference);
        // }

        // Update particle component with camera matrices
        _particleComponent.View = _camera.View;
        _particleComponent.Projection = _camera.Projection;
        _particleComponent.Update(gameTime);

        UpdateChunkLoading();

        UpdateBlockSelection();
        
        UpdateWaterSimulation();

        foreach (var chunk in _chunks.Values)
        {
            chunk.Update(gameTime);
        }
    }

    private void UpdateWaterSimulation()
    {
        _waterSystem.Update(
            GetChunkAtWorldPosition,
            (chunk, x, y, z) => chunk.GetBlock(x, y, z),
            (chunk, x, y, z, block) =>
            {
                chunk.SetBlock(x, y, z, block);
                var chunkPos = new SysVector3(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
                if (_chunks.TryGetValue(chunkPos, out var chunkComponent))
                {
                    chunkComponent.InvalidateGeometry();
                    _meshBuildQueue.Enqueue(chunkComponent);
                }
            }
        );
    }

    private ChunkEntity? GetChunkAtWorldPosition(XnaVector3 worldPos)
    {
        var chunkX = MathF.Floor(worldPos.X / ChunkEntity.Size) * ChunkEntity.Size;
        var chunkZ = MathF.Floor(worldPos.Z / ChunkEntity.Size) * ChunkEntity.Size;
        var chunkPos = new SysVector3(chunkX, 0f, chunkZ);
        
        if (_chunks.TryGetValue(chunkPos, out var chunkComponent))
        {
            return chunkComponent.Chunk;
        }
        
        return null;
    }

    private void ProcessMeshBuildQueue()
    {
        var built = 0;
        while (built < MaxChunkBuildsPerFrame && _meshBuildQueue.TryDequeue(out var chunk))
        {
            chunk.BuildMeshImmediate();
            built++;
        }

        if (_meshBuildQueue.Count > 0)
        {
            _logger.Verbose("Mesh build queue: {Remaining} chunks remaining", _meshBuildQueue.Count);
        }
    }

    private void UpdateChunkLoading()
    {
        if (ChunkGenerator == null)
        {
            return;
        }

        var cameraPos = _camera.Position;
        var playerChunkX = (int)MathF.Floor(cameraPos.X / ChunkEntity.Size);
        var playerChunkZ = (int)MathF.Floor(cameraPos.Z / ChunkEntity.Size);

        var currentPlayerChunk = (playerChunkX, playerChunkZ);

        if (_lastPlayerChunk == currentPlayerChunk)
        {
            return;
        }

        _lastPlayerChunk = currentPlayerChunk;
        _logger.Information("Player moved to chunk ({ChunkX}, {ChunkZ})", playerChunkX, playerChunkZ);

        LoadChunksAroundPlayer(playerChunkX, playerChunkZ);
        UnloadDistantChunks(playerChunkX, playerChunkZ);
    }

    private void LoadChunksAroundPlayer(int centerX, int centerZ)
    {
        var loadedNewChunks = false;

        for (int x = centerX - GenerationDistance; x <= centerX + GenerationDistance; x++)
        {
            for (int z = centerZ - GenerationDistance; z <= centerZ + GenerationDistance; z++)
            {
                var chunkPos = new SysVector3(x * ChunkEntity.Size, 0f, z * ChunkEntity.Size);

                if (!_chunks.ContainsKey(chunkPos))
                {
                    if (ChunkGeneratorAsync != null)
                    {
                        _ = RequestChunkFromServerAsync(x, z);
                    }
                    else if (ChunkGenerator != null)
                    {
                        var chunk = ChunkGenerator(x, z);
                        _ = AddChunkAsync(chunk);
                    }
                    
                    loadedNewChunks = true;
                }
            }
        }

        if (loadedNewChunks)
        {
            foreach (var chunk in _chunks.Values)
            {
                if (chunk.HasMesh)
                {
                    chunk.InvalidateGeometry();
                    _meshBuildQueue.Enqueue(chunk);
                }
            }
        }
    }

    private async Task RequestChunkFromServerAsync(int chunkX, int chunkZ)
    {
        try
        {
            _logger.Debug("Requesting chunk ({X}, {Z}) from server", chunkX, chunkZ);
            
            var chunk = await ChunkGeneratorAsync!(chunkX, chunkZ);
            
            await AddChunkAsync(chunk);
            
            _logger.Debug("Chunk ({X}, {Z}) received from server", chunkX, chunkZ);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load chunk ({X}, {Z}) from server", chunkX, chunkZ);
        }
    }

    private void UnloadDistantChunks(int centerX, int centerZ)
    {
        var unloadDistance = GenerationDistance + 1;
        var chunksToRemove = new List<SysVector3>();

        foreach (var (pos, _) in _chunks)
        {
            var chunkX = (int)(pos.X / ChunkEntity.Size);
            var chunkZ = (int)(pos.Z / ChunkEntity.Size);

            var distanceX = Math.Abs(chunkX - centerX);
            var distanceZ = Math.Abs(chunkZ - centerZ);

            if (distanceX > unloadDistance || distanceZ > unloadDistance)
            {
                chunksToRemove.Add(pos);
            }
        }

        foreach (var pos in chunksToRemove)
        {
            RemoveChunk(pos);
        }

        if (chunksToRemove.Count > 0)
        {
            _logger.Information("Unloaded {Count} distant chunks", chunksToRemove.Count);
        }
    }

    private void UpdateBlockSelection()
    {
        var ray = _camera.GetPickRay();
        SelectedBlock = RaycastBlock(ray);
    }

    public (ChunkComponent? Chunk, int X, int Y, int Z)? RaycastBlock(Ray ray)
    {
        var step = 0.1f;
        var currentDistance = 0f;

        while (currentDistance < MaxRaycastDistance)
        {
            var point = ray.Position + ray.Direction * currentDistance;

            foreach (var chunk in _chunks.Values)
            {
                if (chunk.Chunk == null)
                {
                    continue;
                }

                var chunkPos = chunk.Position;
                var relativePos = point - chunkPos;

                var blockX = (int)MathF.Floor(relativePos.X);
                var blockY = (int)MathF.Floor(relativePos.Y);
                var blockZ = (int)MathF.Floor(relativePos.Z);

                if (blockX >= 0 && blockX < ChunkEntity.Size &&
                    blockY >= 0 && blockY < ChunkEntity.Height &&
                    blockZ >= 0 && blockZ < ChunkEntity.Size)
                {
                    var block = chunk.Chunk.GetBlock(blockX, blockY, blockZ);
                    
                    if (block != null && block.BlockType != Game.Data.Types.BlockType.Air)
                    {
                        return (chunk, blockX, blockY, blockZ);
                    }
                }
            }

            currentDistance += step;
        }

        return null;
    }

    public void SpawnParticles(XnaVector3 position, int count, float spread = 1f, float speed = 5f, float lifeTime = 2f, Color? color = null)
    {
        _particleComponent.SpawnParticles(position, count, spread, speed, lifeTime, color);
    }

    public void InvalidateBlockAndAdjacentChunks(ChunkComponent chunk, int blockX, int blockY, int blockZ)
    {
        if (chunk == null || chunk.Chunk == null)
            return;

        var affectedChunks = new HashSet<ChunkComponent> { chunk };

        // Check if the block is on the edge of the chunk and add adjacent chunks
        var chunkSize = ChunkEntity.Size;

        // Check each face of the block
        var neighborOffsets = new[]
        {
            (1, 0, 0),   // East
            (-1, 0, 0),  // West
            (0, 1, 0),   // Up
            (0, -1, 0),  // Down
            (0, 0, 1),   // North
            (0, 0, -1)   // South
        };

        foreach (var (dx, dy, dz) in neighborOffsets)
        {
            var neighborX = blockX + dx;
            var neighborY = blockY + dy;
            var neighborZ = blockZ + dz;

            // If neighbor is outside current chunk bounds, find the adjacent chunk
            if (neighborX < 0 || neighborX >= chunkSize ||
                neighborY < 0 || neighborY >= ChunkEntity.Height ||
                neighborZ < 0 || neighborZ >= chunkSize)
            {
                // Calculate which chunk the neighbor block belongs to
                var worldX = chunk.Chunk.Position.X + neighborX;
                var worldY = chunk.Chunk.Position.Y + neighborY;
                var worldZ = chunk.Chunk.Position.Z + neighborZ;

                var neighborChunkX = (int)MathF.Floor(worldX / chunkSize) * chunkSize;
                var neighborChunkZ = (int)MathF.Floor(worldZ / chunkSize) * chunkSize;

                var neighborChunkPos = new SysVector3(neighborChunkX, 0, neighborChunkZ);

                if (_chunks.TryGetValue(neighborChunkPos, out var neighborChunk))
                {
                    affectedChunks.Add(neighborChunk);
                }
            }
        }

        // Invalidate all affected chunks
        foreach (var affectedChunk in affectedChunks)
        {
            affectedChunk.InvalidateGeometry();
            _meshBuildQueue.Enqueue(affectedChunk);

            _logger.Debug("Chunk invalidated at {Position}", affectedChunk.Position);
        }

        // Recalculate cross-chunk lighting for all affected chunks
        if (affectedChunks.Any())
        {
            var affectedChunkEntities = affectedChunks
                .Where(c => c.Chunk != null)
                .Select(c => c.Chunk!)
                .ToList();

            _lightSystem.CalculateCrossChunkLighting(affectedChunkEntities, GetChunkEntityForLighting);

            _logger.Debug("Cross-chunk lighting recalculated for {Count} chunks", affectedChunks.Count);
        }
        
        QueueWaterUpdatesAroundBlock(chunk.Chunk, blockX, blockY, blockZ);
    }

    public void QueueWaterUpdatesAroundBlock(ChunkEntity chunk, int x, int y, int z)
    {
        _waterSystem.QueueWaterUpdate(chunk, x, y - 1, z);
        _waterSystem.QueueWaterUpdate(chunk, x + 1, y, z);
        _waterSystem.QueueWaterUpdate(chunk, x - 1, y, z);
        _waterSystem.QueueWaterUpdate(chunk, x, y, z + 1);
        _waterSystem.QueueWaterUpdate(chunk, x, y, z - 1);
        _waterSystem.QueueWaterUpdate(chunk, x, y + 1, z);
    }

    private ChunkEntity? GetChunkEntityForLighting(int chunkX, int chunkZ)
    {
        var chunkPos = new SysVector3(chunkX * ChunkEntity.Size, 0f, chunkZ * ChunkEntity.Size);
        return GetChunkEntity(new XnaVector3(chunkPos.X, chunkPos.Y, chunkPos.Z));
    }

    public void InvalidateChunkGeometry(ChunkComponent chunk)
    {
        if (chunk != null)
        {
            chunk.InvalidateGeometry();
            _meshBuildQueue.Enqueue(chunk);
            _logger.Debug("Chunk geometry invalidated and queued for rebuild at {Position}", chunk.Position);

            // Also invalidate neighboring chunks for proper face culling and lighting
            var affectedChunks = new HashSet<ChunkComponent> { chunk };
            InvalidateNeighborChunks(chunk, affectedChunks);

            // Recalculate cross-chunk lighting for all affected chunks
            var affectedChunkEntities = affectedChunks
                .Where(c => c.Chunk != null)
                .Select(c => c.Chunk!)
                .ToList();

            _lightSystem.CalculateCrossChunkLighting(affectedChunkEntities, GetChunkEntityForLighting);
            _logger.Debug("Cross-chunk lighting recalculated for {Count} chunks", affectedChunks.Count);
        }
    }

    private void InvalidateNeighborChunks(ChunkComponent chunk, HashSet<ChunkComponent> affectedChunks)
    {
        var chunkPos = new SysVector3(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
        var chunkSize = (float)ChunkEntity.Size;

        // Check all 4 neighboring positions (horizontal neighbors for lighting)
        var neighborOffsets = new[]
        {
            new SysVector3(chunkSize, 0, 0),   // East
            new SysVector3(-chunkSize, 0, 0),  // West
            new SysVector3(0, 0, chunkSize),   // North
            new SysVector3(0, 0, -chunkSize)   // South
        };

        foreach (var offset in neighborOffsets)
        {
            var neighborPos = chunkPos + offset;
            if (_chunks.TryGetValue(neighborPos, out var neighborChunk))
            {
                if (affectedChunks.Add(neighborChunk))
                {
                    neighborChunk.InvalidateGeometry();
                    _meshBuildQueue.Enqueue(neighborChunk);
                    _logger.Debug("Neighbor chunk invalidated at {Position}", neighborPos);
                }
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        if (EnableFrustumCulling)
        {
            _frustum = new BoundingFrustum(_camera.View * _camera.Projection);
        }

        var cameraPosition = _camera.Position;
        var visibleChunks = 0;
        var culledChunks = 0;

        foreach (var chunk in _chunks.Values)
        {
            if (ShouldRenderChunk(chunk, cameraPosition))
            {
                DrawChunk(chunk, gameTime);
                visibleChunks++;
            }
            else
            {
                culledChunks++;
            }
        }

        if (culledChunks > 0)
        {
            _logger.Verbose("Rendered {Visible} chunks, culled {Culled} chunks", visibleChunks, culledChunks);
        }

        // Draw particles after chunks
        _particleComponent.Draw3d(gameTime);
    }

    private bool ShouldRenderChunk(ChunkComponent chunk, XnaVector3 cameraPosition)
    {
        if (chunk.Chunk == null)
        {
            return false;
        }

        var chunkPos = chunk.Position;
        var chunkCenter = new XnaVector3(
            chunkPos.X + ChunkEntity.Size * 0.5f,
            chunkPos.Y + ChunkEntity.Height * 0.5f,
            chunkPos.Z + ChunkEntity.Size * 0.5f
        );

        var distance = XnaVector3.Distance(cameraPosition, chunkCenter);
        if (distance > ViewRange)
        {
            return false;
        }

        if (EnableFrustumCulling && _frustum != null)
        {
            var chunkRadius = MathF.Sqrt(
                ChunkEntity.Size * ChunkEntity.Size +
                ChunkEntity.Height * ChunkEntity.Height +
                ChunkEntity.Size * ChunkEntity.Size
            ) * 0.5f;

            var chunkSphere = new BoundingSphere(chunkCenter, chunkRadius);

            if (_frustum.Contains(chunkSphere) == ContainmentType.Disjoint)
            {
                return false;
            }
        }

        return true;
    }

    private void DrawChunk(ChunkComponent chunk, GameTime gameTime)
    {
        if (chunk.Chunk == null)
        {
            return;
        }

        chunk.DrawWithCamera(gameTime, _camera.View, _camera.Projection);
    }

    private void ProcessPendingChunks()
    {
        while (_pendingChunks.TryDequeue(out var pending))
        {
            var (position, chunk) = pending;

            if (_chunks.ContainsKey(position))
            {
                _logger.Warning("Chunk at position {Position} already exists, skipping", position);
                continue;
            }

            var chunkComponent = new ChunkComponent
            {
                AutoRotate = false,
                BlockScale = 1f,
                RenderTransparentBlocks = true,
                GetNeighborChunk = GetChunkEntity
            };

            chunkComponent.SetChunk(chunk);
            // chunkComponent.SetDayNightCycle(_dayNightCycle);

            // Calculate initial lighting for the new chunk
            // For now, use single-chunk lighting for new chunks
            _lightSystem.CalculateInitialSunlight(chunk);

            if (_chunks.TryAdd(position, chunkComponent))
            {
                _meshBuildQueue.Enqueue(chunkComponent);
                _logger.Information("Chunk added at position {Position}, queued for mesh build", position);
            }
            else
            {
                _logger.Warning("Failed to add chunk at position {Position}", position);
                chunkComponent.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        ClearChunks();
    }
}
