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
    
    private readonly CameraComponent _camera;
    private bool _isDisposed;

    public WorldComponent(GraphicsDevice graphicsDevice, CameraComponent camera)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
    }

    public CameraComponent Camera => _camera;

    public IReadOnlyDictionary<SysVector3, ChunkComponent> Chunks => _chunks;

    public async Task AddChunkAsync(ChunkEntity chunk)
    {
        if (chunk == null)
        {
            throw new ArgumentNullException(nameof(chunk));
        }

        var chunkPosition = chunk.Position;

        await Task.Run(() =>
        {
            _pendingChunks.Enqueue((chunkPosition, chunk));
            _logger.Debug("Chunk queued for addition at position {Position}", chunkPosition);
        });
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

        _camera.Update(gameTime);

        foreach (var chunk in _chunks.Values)
        {
            chunk.Update(gameTime);
        }
    }

    public void Draw(GameTime gameTime)
    {
        foreach (var chunk in _chunks.Values)
        {
            DrawChunk(chunk, gameTime);
        }
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
                RenderTransparentBlocks = false
            };

            chunkComponent.SetChunk(chunk);

            if (_chunks.TryAdd(position, chunkComponent))
            {
                _logger.Information("Chunk added at position {Position}", position);
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
