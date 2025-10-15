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
    private bool _isDisposed;
    private BoundingFrustum? _frustum;
    private (int X, int Z)? _lastPlayerChunk;

    public WorldComponent(GraphicsDevice graphicsDevice, CameraComponent camera)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
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
        return block != null && block.BlockType != Game.Data.Types.BlockType.Air;
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

        UpdateChunkLoading();

        UpdateBlockSelection();

        foreach (var chunk in _chunks.Values)
        {
            chunk.Update(gameTime);
        }
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
