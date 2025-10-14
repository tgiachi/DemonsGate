using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using Serilog;
using SquidCraft.Client.Context;
using SquidCraft.Client.Services;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Client.Components;

/// <summary>
/// Builds and renders a chunk mesh from <see cref="ChunkEntity"/> data using block textures provided by <see cref="BlockManagerService"/>.
/// Serves as the rendering foundation for future world streaming and chunk management.
/// </summary>
public sealed class ChunkComponent : IDisposable
{
    private static readonly Dictionary<SideType, (int X, int Y, int Z)> NeighborOffsets = new()
    {
        { SideType.Top, (0, 1, 0) },
        { SideType.Bottom, (0, -1, 0) },
        { SideType.North, (0, 0, -1) },
        { SideType.South, (0, 0, 1) },
        { SideType.East, (1, 0, 0) },
        { SideType.West, (-1, 0, 0) }
    };

    private readonly GraphicsDevice _graphicsDevice;
    private readonly BlockManagerService _blockManagerService;
    private readonly BasicEffect _effect;
    private readonly ILogger _logger = Log.ForContext<ChunkComponent>();

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private Texture2D? _texture;
    private bool _geometryInvalidated = true;
    private int _primitiveCount;

    private ChunkEntity? _chunk;
    private float _rotationY;
    private readonly Vector3 _chunkCenter = new(ChunkEntity.Size / 2f, ChunkEntity.Height / 2f, ChunkEntity.Size / 2f);
    private Vector3? _customCameraTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkComponent"/> class.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device used for rendering.</param>
    /// <param name="blockManagerService">Service that resolves block textures and metadata.</param>
    public ChunkComponent()
    {
        _graphicsDevice = SquidCraftClientContext.GraphicsDevice;
        _blockManagerService = SquidCraftClientContext.BlockManagerService;

        _effect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = false,
            VertexColorEnabled = false
        };
    }

    /// <summary>
    /// Gets the chunk currently bound to the component.
    /// </summary>
    public ChunkEntity? Chunk => _chunk;

    /// <summary>
    /// Gets or sets the translation applied to the chunk in world space.
    /// Defaults to the chunk origin derived from <see cref="ChunkEntity.Position"/>.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the manual rotation applied to the chunk (Yaw, Pitch, Roll).
    /// </summary>
    public Vector3 ManualRotation { get; set; } = Vector3.Zero;

    /// <summary>
    /// Enables a simple idle rotation animation around the Y axis.
    /// </summary>
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Rotation speed in radians per second when <see cref="AutoRotate"/> is enabled.
    /// </summary>
    public float RotationSpeed { get; set; } = MathHelper.ToRadians(10f);

    /// <summary>
    /// Gets or sets the camera position used when drawing the chunk.
    /// </summary>
    public Vector3 CameraPosition { get; set; } = new(35f, 45f, 35f);

    /// <summary>
    /// Gets or sets the camera target for chunk rendering.
    /// </summary>
    public Vector3 CameraTarget
    {
        get => _customCameraTarget ?? DefaultCameraTarget;
        set => _customCameraTarget = value;
    }

    private Vector3 DefaultCameraTarget => Position + _chunkCenter * BlockScale;

    /// <summary>
    /// Gets or sets the uniform block scale applied during rendering.
    /// </summary>
    public float BlockScale { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether transparent blocks (e.g. water) should be rendered.
    /// </summary>
    public bool RenderTransparentBlocks { get; set; } = false;

    /// <summary>
    /// Binds a chunk to the component and schedules a geometry rebuild.
    /// </summary>
    /// <param name="chunk">Chunk to render.</param>
    public void SetChunk(ChunkEntity chunk)
    {
        _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        Position = new Vector3(chunk.Position.X, chunk.Position.Y, chunk.Position.Z);
        _customCameraTarget = null; // Reset to automatic center tracking
        InvalidateGeometry();
    }

    /// <summary>
    /// Signals that the underlying chunk data changed and geometry needs to be recreated.
    /// </summary>
    public void InvalidateGeometry() => _geometryInvalidated = true;

    /// <summary>
    /// Updates the component state (handles optional auto rotation).
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    public void Update(GameTime gameTime)
    {
        if (!AutoRotate)
        {
            return;
        }

        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _rotationY = (_rotationY + RotationSpeed * elapsedSeconds) % MathHelper.TwoPi;
    }

    /// <summary>
    /// Draws the chunk mesh using the configured camera and textures.
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    public void Draw(GameTime gameTime)
    {
        var viewport = _graphicsDevice.Viewport;
        var aspectRatio = viewport.AspectRatio <= 0 ? 1f : viewport.AspectRatio;

        var lookTarget = _customCameraTarget ?? DefaultCameraTarget;
        var view = Matrix.CreateLookAt(CameraPosition, lookTarget, Vector3.Up);
        var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 500f);

        DrawWithCamera(gameTime, view, projection);
    }

    /// <summary>
    /// Draws the chunk mesh using external view and projection matrices.
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    /// <param name="view">View matrix from camera.</param>
    /// <param name="projection">Projection matrix from camera.</param>
    public void DrawWithCamera(GameTime gameTime, Matrix view, Matrix projection)
    {
        EnsureGeometry();

        if (_vertexBuffer == null || _indexBuffer == null || _texture == null || _primitiveCount == 0)
        {
            return;
        }

        var rotation = Matrix.CreateFromYawPitchRoll(_rotationY + ManualRotation.Y, ManualRotation.X, ManualRotation.Z);
        var world =
            Matrix.CreateTranslation(-_chunkCenter) *
            Matrix.CreateScale(BlockScale) *
            rotation *
            Matrix.CreateTranslation(_chunkCenter + Position);

        _effect.World = world;
        _effect.View = view;
        _effect.Projection = projection;
        _effect.Texture = _texture;

        var previousBlendState = _graphicsDevice.BlendState;
        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        var previousRasterizerState = _graphicsDevice.RasterizerState;
        var previousSamplerState = _graphicsDevice.SamplerStates[0];

        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.Indices = null;

        _graphicsDevice.BlendState = previousBlendState;
        _graphicsDevice.DepthStencilState = previousDepthStencilState;
        _graphicsDevice.RasterizerState = previousRasterizerState;
        _graphicsDevice.SamplerStates[0] = previousSamplerState;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ClearGeometry();
        _effect.Dispose();
    }

    private void EnsureGeometry()
    {
        if (!_geometryInvalidated)
        {
            return;
        }

        if (_chunk == null)
        {
            ClearGeometry();
            _geometryInvalidated = false;
            return;
        }

        var vertices = new List<VertexPositionTexture>();
        var indices = new List<int>();
        Texture2D? atlasTexture = null;

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int y = 0; y < ChunkEntity.Height; y++)
            {
                for (int z = 0; z < ChunkEntity.Size; z++)
                {
                    var block = _chunk.Blocks[ChunkEntity.GetIndex(x, y, z)];

                    if (block == null || block.BlockType == BlockType.Air)
                    {
                        continue;
                    }

                    var definition = _blockManagerService.GetBlockDefinition(block.BlockType);

                    if (definition == null)
                    {
                        continue;
                    }

                    if (definition.IsTransparent && !RenderTransparentBlocks)
                    {
                        continue;
                    }

                    foreach (var side in Enum.GetValues<SideType>())
                    {
                        if (!ShouldRenderFace(x, y, z, side))
                        {
                            continue;
                        }

                        var region = _blockManagerService.GetBlockSide(block.BlockType, side);

                        if (region == null)
                        {
                            continue;
                        }

                        atlasTexture ??= region.Texture;

                        var uv = ExtractUv(region);
                        var faceVertices = GetFaceVertices(side, x, y, z, uv);

                        var baseIndex = vertices.Count;
                        vertices.AddRange(faceVertices);
                        indices.AddRange(new[]
                        {
                            baseIndex,
                            baseIndex + 1,
                            baseIndex + 2,
                            baseIndex + 2,
                            baseIndex + 3,
                            baseIndex
                        });
                    }
                }
            }
        }

        if (vertices.Count == 0 || indices.Count == 0 || atlasTexture == null)
        {
            ClearGeometry();
            _geometryInvalidated = false;
            return;
        }

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionTexture), vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());

        _texture = atlasTexture;
        _primitiveCount = indices.Count / 3;

        _geometryInvalidated = false;
        _logger.Information("Chunk geometry rebuilt: {Vertices} vertices, {Faces} faces", vertices.Count, indices.Count / 6);
    }

    private bool ShouldRenderFace(int x, int y, int z, SideType side)
    {
        var (offsetX, offsetY, offsetZ) = NeighborOffsets[side];
        var neighborX = x + offsetX;
        var neighborY = y + offsetY;
        var neighborZ = z + offsetZ;

        if (!IsWithinChunk(neighborX, neighborY, neighborZ))
        {
            return true;
        }

        var neighbor = _chunk!.Blocks[ChunkEntity.GetIndex(neighborX, neighborY, neighborZ)];

        if (neighbor == null || neighbor.BlockType == BlockType.Air)
        {
            return true;
        }

        return _blockManagerService.IsTransparent(neighbor.BlockType);
    }

    private static bool IsWithinChunk(int x, int y, int z)
    {
        return x >= 0 && x < ChunkEntity.Size &&
               y >= 0 && y < ChunkEntity.Height &&
               z >= 0 && z < ChunkEntity.Size;
    }

    private static VertexPositionTexture[] GetFaceVertices(SideType side, int blockX, int blockY, int blockZ, (Vector2 Min, Vector2 Max) uv)
    {
        var (min, max) = uv;
        float x = blockX;
        float y = blockY;
        float z = blockZ;
        float x1 = blockX + 1f;
        float y1 = blockY + 1f;
        float z1 = blockZ + 1f;

        return side switch
        {
            SideType.Top => new[]
            {
                new VertexPositionTexture(new Vector3(x, y1, z), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(x, y1, z1), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y1, z1), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y1, z), new Vector2(max.X, min.Y))
            },
            SideType.Bottom => new[]
            {
                new VertexPositionTexture(new Vector3(x, y, z1), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(x, y, z), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y, z), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y, z1), new Vector2(max.X, min.Y))
            },
            SideType.North => new[]
            {
                new VertexPositionTexture(new Vector3(x, y1, z), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(x, y, z), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y, z), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y1, z), new Vector2(max.X, min.Y))
            },
            SideType.South => new[]
            {
                new VertexPositionTexture(new Vector3(x1, y1, z1), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(x1, y, z1), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(x, y, z1), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(x, y1, z1), new Vector2(max.X, min.Y))
            },
            SideType.East => new[]
            {
                new VertexPositionTexture(new Vector3(x1, y1, z), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(x1, y, z), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y, z1), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(x1, y1, z1), new Vector2(max.X, min.Y))
            },
            SideType.West => new[]
            {
                new VertexPositionTexture(new Vector3(x, y1, z1), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(x, y, z1), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(x, y, z), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(x, y1, z), new Vector2(max.X, min.Y))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unsupported side type")
        };
    }

    private static (Vector2 Min, Vector2 Max) ExtractUv(Texture2DRegion region)
    {
        var texture = region.Texture;
        var bounds = region.Bounds;

        const float inset = 0.001f;

        var minX = (bounds.X + inset) / texture.Width;
        var minY = (bounds.Y + inset) / texture.Height;
        var maxX = (bounds.X + bounds.Width - inset) / texture.Width;
        var maxY = (bounds.Y + bounds.Height - inset) / texture.Height;

        return (new Vector2(minX, minY), new Vector2(maxX, maxY));
    }

    private void ClearGeometry()
    {
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;

        _indexBuffer?.Dispose();
        _indexBuffer = null;

        _texture = null;
        _primitiveCount = 0;
    }
}
