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

    private float _opacity = 0f;
    private float _targetOpacity = 1f;
    private bool _isFadingIn;

    public Func<Vector3, ChunkEntity?>? GetNeighborChunk { get; set; }

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
            VertexColorEnabled = true
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

    public float Opacity
    {
        get => _opacity;
        set => _opacity = MathHelper.Clamp(value, 0f, 1f);
    }

    public float FadeInSpeed { get; set; } = 2f;

    public bool EnableFadeIn { get; set; } = true;

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

        if (EnableFadeIn)
        {
            _opacity = 0f;
            _isFadingIn = true;
        }
        else
        {
            _opacity = 1f;
            _isFadingIn = false;
        }
    }

    /// <summary>
    /// Signals that the underlying chunk data changed and geometry needs to be recreated.
    /// </summary>
    public void InvalidateGeometry() => _geometryInvalidated = true;

    public bool HasMesh => _vertexBuffer != null;

    public void BuildMeshImmediate()
    {
        if (!_geometryInvalidated)
        {
            return;
        }

        EnsureGeometry();

        if (EnableFadeIn && _opacity < 0.01f)
        {
            _opacity = 0f;
            _isFadingIn = true;
        }
    }

    /// <summary>
    /// Updates the component state (handles optional auto rotation).
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    public void Update(GameTime gameTime)
    {
        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_isFadingIn)
        {
            _opacity += FadeInSpeed * elapsedSeconds;
            if (_opacity >= _targetOpacity)
            {
                _opacity = _targetOpacity;
                _isFadingIn = false;
            }
        }

        if (AutoRotate)
        {
            _rotationY = (_rotationY + RotationSpeed * elapsedSeconds) % MathHelper.TwoPi;
        }
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
        if (_vertexBuffer == null || _indexBuffer == null || _texture == null || _primitiveCount == 0)
        {
            return;
        }

        if (_opacity <= 0f)
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
        _effect.Alpha = _opacity;

        var previousBlendState = _graphicsDevice.BlendState;
        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        var previousRasterizerState = _graphicsDevice.RasterizerState;
        var previousSamplerState = _graphicsDevice.SamplerStates[0];

        var needsBlending = RenderTransparentBlocks || _opacity < 1f;
        _graphicsDevice.BlendState = needsBlending ? BlendState.AlphaBlend : BlendState.Opaque;
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

        var vertices = new List<VertexPositionColorTexture>();
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
                        var faceColor = CalculateFaceColor(x, y, z, side);
                        var blockHeight = definition.Height;
                        var faceVertices = GetFaceVertices(side, x, y, z, uv, faceColor, blockHeight);

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

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());

        _texture = atlasTexture;
        _primitiveCount = indices.Count / 3;

        _geometryInvalidated = false;
        _logger.Debug("Chunk geometry rebuilt: {Vertices} vertices, {Faces} faces", vertices.Count, indices.Count / 6);
    }

    private bool ShouldRenderFace(int x, int y, int z, SideType side)
    {
        var currentBlock = _chunk!.Blocks[ChunkEntity.GetIndex(x, y, z)];
        if (currentBlock == null)
        {
            return false;
        }

        var (offsetX, offsetY, offsetZ) = NeighborOffsets[side];
        var neighborX = x + offsetX;
        var neighborY = y + offsetY;
        var neighborZ = z + offsetZ;

        if (!IsWithinChunk(neighborX, neighborY, neighborZ))
        {
            return ShouldRenderCrossChunkFace(x, y, z, side, currentBlock.BlockType);
        }

        var neighbor = _chunk!.Blocks[ChunkEntity.GetIndex(neighborX, neighborY, neighborZ)];

        if (neighbor == null || neighbor.BlockType == BlockType.Air)
        {
            return true;
        }

        if (currentBlock.BlockType == neighbor.BlockType)
        {
            var currentDef = _blockManagerService.GetBlockDefinition(currentBlock.BlockType);
            if (currentDef != null && currentDef.IsLiquid)
            {
                return false;
            }
        }

        return _blockManagerService.IsTransparent(neighbor.BlockType);
    }

    private bool ShouldRenderCrossChunkFace(int x, int y, int z, SideType side, BlockType currentBlockType)
    {
        if (GetNeighborChunk == null || _chunk == null)
        {
            return true;
        }

        var (offsetX, offsetY, offsetZ) = NeighborOffsets[side];
        var worldX = _chunk.Position.X + x + offsetX;
        var worldY = _chunk.Position.Y + y + offsetY;
        var worldZ = _chunk.Position.Z + z + offsetZ;

        var neighborChunkX = MathF.Floor(worldX / ChunkEntity.Size) * ChunkEntity.Size;
        var neighborChunkZ = MathF.Floor(worldZ / ChunkEntity.Size) * ChunkEntity.Size;
        var neighborChunkPos = new Vector3(neighborChunkX, 0f, neighborChunkZ);

        var neighborChunk = GetNeighborChunk(neighborChunkPos);
        if (neighborChunk == null)
        {
            return true;
        }

        var localX = (int)(worldX - neighborChunk.Position.X);
        var localY = (int)(worldY - neighborChunk.Position.Y);
        var localZ = (int)(worldZ - neighborChunk.Position.Z);

        if (localX < 0 || localX >= ChunkEntity.Size ||
            localY < 0 || localY >= ChunkEntity.Height ||
            localZ < 0 || localZ >= ChunkEntity.Size)
        {
            return true;
        }

        var neighborBlock = neighborChunk.GetBlock(localX, localY, localZ);

        if (neighborBlock == null || neighborBlock.BlockType == BlockType.Air)
        {
            return true;
        }

        if (currentBlockType == neighborBlock.BlockType)
        {
            var currentDef = _blockManagerService.GetBlockDefinition(currentBlockType);
            if (currentDef != null && currentDef.IsLiquid)
            {
                return false;
            }
        }

        return _blockManagerService.IsTransparent(neighborBlock.BlockType);
    }

    private static bool IsWithinChunk(int x, int y, int z)
    {
        return x >= 0 && x < ChunkEntity.Size &&
               y >= 0 && y < ChunkEntity.Height &&
               z >= 0 && z < ChunkEntity.Size;
    }

    private static Color CalculateFaceColor(int x, int y, int z, SideType side)
    {
        var ambientOcclusion = 1.0f;

        switch (side)
        {
            case SideType.Top:
                ambientOcclusion = 1.0f;
                break;
            case SideType.Bottom:
                ambientOcclusion = 0.5f;
                break;
            case SideType.North:
            case SideType.South:
                ambientOcclusion = 0.8f;
                break;
            case SideType.East:
            case SideType.West:
                ambientOcclusion = 0.75f;
                break;
        }

        return new Color(ambientOcclusion, ambientOcclusion, ambientOcclusion, 1.0f);
    }

    private static VertexPositionColorTexture[] GetFaceVertices(SideType side, int blockX, int blockY, int blockZ, (Vector2 Min, Vector2 Max) uv, Color color, float height = 1.0f)
    {
        var (min, max) = uv;
        float x = blockX;
        float y = blockY;
        float z = blockZ;
        float x1 = blockX + 1f;
        float y1 = blockY + height;
        float z1 = blockZ + 1f;

        return side switch
        {
            SideType.Top => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y1, z), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y1, z1), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z1), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z), color, new Vector2(max.X, min.Y))
            },
            SideType.Bottom => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y, z1), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z1), color, new Vector2(max.X, min.Y))
            },
            SideType.North => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y1, z), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z), color, new Vector2(max.X, min.Y))
            },
            SideType.South => new[]
            {
                new VertexPositionColorTexture(new Vector3(x1, y1, z1), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z1), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z1), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y1, z1), color, new Vector2(max.X, min.Y))
            },
            SideType.East => new[]
            {
                new VertexPositionColorTexture(new Vector3(x1, y1, z), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y, z1), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x1, y1, z1), color, new Vector2(max.X, min.Y))
            },
            SideType.West => new[]
            {
                new VertexPositionColorTexture(new Vector3(x, y1, z1), color, new Vector2(min.X, min.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z1), color, new Vector2(min.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y, z), color, new Vector2(max.X, max.Y)),
                new VertexPositionColorTexture(new Vector3(x, y1, z), color, new Vector2(max.X, min.Y))
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
