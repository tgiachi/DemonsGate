using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using Serilog;
using SquidCraft.Client.Context;
using SquidCraft.Client.Services;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Client.Components;

/// <summary>
/// Renders a textured block in 3D space using the textures provided by <see cref="BlockManagerService"/>.
/// Intended as the building primitive for future chunk rendering.
/// </summary>
public sealed class Block3DComponent : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BlockManagerService _blockManagerService;
    private readonly BasicEffect _effect;
    private readonly ILogger _logger = Log.ForContext<Block3DComponent>();

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;
    private bool _geometryInvalidated = true;
    private Texture2D? _currentAtlasTexture;

    private float _rotationY;
    private Matrix _lastWorld = Matrix.Identity;
    private Matrix _lastView = Matrix.Identity;
    private Matrix _lastProjection = Matrix.Identity;

    private SpriteFontBase? _labelFont;

    /// <summary>
    /// Initializes a new instance of <see cref="Block3DComponent"/>.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device used for rendering.</param>
    /// <param name="blockManagerService">Service responsible for resolving block textures.</param>
    public Block3DComponent(GraphicsDevice graphicsDevice, BlockManagerService blockManagerService)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _blockManagerService = blockManagerService ?? throw new ArgumentNullException(nameof(blockManagerService));

        _effect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = false,
            VertexColorEnabled = false
        };

        BlockType = BlockType.Grass;
    }

    /// <summary>
    /// Gets or sets the semantic block type to render.
    /// </summary>
    public BlockType BlockType
    {
        get => _blockType;
        init
        {
            if (_blockType != value)
            {
                _blockType = value;
                _geometryInvalidated = true;
            }
        }
    }
    private readonly BlockType _blockType;

    /// <summary>
    /// Gets or sets the block size (edge length) in world units.
    /// </summary>
    public float Size
    {
        get => _size;
        init
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Size), "Block size must be greater than zero.");
            }

            if (Math.Abs(_size - value) > float.Epsilon)
            {
                _size = value;
                _geometryInvalidated = true;
            }
        }
    }
    private readonly float _size = 1f;

    /// <summary>
    /// Gets or sets the block position in world space.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets a manual rotation applied to the block (Yaw, Pitch, Roll).
    /// </summary>
    public Vector3 ManualRotation { get; set; } = Vector3.Zero;

    /// <summary>
    /// Enables a simple idle rotation animation around the Y axis.
    /// </summary>
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Rotation speed in radians per second when <see cref="AutoRotate"/> is enabled.
    /// </summary>
    public float RotationSpeed { get; set; } = MathHelper.ToRadians(25f);

    /// <summary>
    /// Camera position used when drawing the block.
    /// </summary>
    public Vector3 CameraPosition { get; set; } = new(3f, 3f, 3f);

    /// <summary>
    /// Target that the camera looks at. Defaults to the block position.
    /// </summary>
    public Vector3 CameraTarget { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets whether side labels should be rendered on top of the block.
    /// </summary>
    public bool ShowSideLabels { get; set; }

    /// <summary>
    /// Font size used when drawing the side labels.
    /// </summary>
    public int LabelFontSize
    {
        get => _labelFontSize;
        set
        {
            if (_labelFontSize != value)
            {
                _labelFontSize = Math.Max(8, value);
                _labelFont = null;
            }
        }
    }
    private int _labelFontSize = 14;

    /// <summary>
    /// Color applied to the side labels.
    /// </summary>
    public Color LabelColor { get; set; } = Color.White;

    /// <summary>
    /// Updates the component state (e.g. handles auto rotation).
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
    /// Draws the 3D block using the configured camera and textures.
    /// </summary>
    /// <param name="gameTime">Elapsed time information.</param>
    public void Draw3D(GameTime gameTime)
    {
        EnsureGeometry();

        if (_vertexBuffer == null || _indexBuffer == null || _currentAtlasTexture == null)
        {
            return;
        }

        var viewport = _graphicsDevice.Viewport;
        var aspectRatio = viewport.AspectRatio <= 0 ? 1f : viewport.AspectRatio;

        _lastWorld = Matrix.CreateScale(Size)
                     * Matrix.CreateFromYawPitchRoll(_rotationY + ManualRotation.Y, ManualRotation.X, ManualRotation.Z)
                     * Matrix.CreateTranslation(Position);

        var lookTarget = CameraTarget == Vector3.Zero ? Position : CameraTarget;

        _lastView = Matrix.CreateLookAt(CameraPosition, lookTarget, Vector3.Up);
        _lastProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 100f);

        _effect.Texture = _currentAtlasTexture;
        _effect.World = _lastWorld;
        _effect.View = _lastView;
        _effect.Projection = _lastProjection;

        var previousBlendState = _graphicsDevice.BlendState;
        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        var previousRasterizerState = _graphicsDevice.RasterizerState;
        var previousSamplerState = _graphicsDevice.SamplerStates[0];

        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        _graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.Indices = null;

        _graphicsDevice.BlendState = previousBlendState;
        _graphicsDevice.DepthStencilState = previousDepthStencilState;
        _graphicsDevice.RasterizerState = previousRasterizerState;
        _graphicsDevice.SamplerStates[0] = previousSamplerState;
    }

    /// <summary>
    /// Draws optional labels describing each block side. Requires a begun sprite batch.
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch currently in drawing state.</param>
    public void DrawLabels(SpriteBatch spriteBatch)
    {
        if (!ShowSideLabels)
        {
            return;
        }

        _labelFont ??= SquidCraftClientContext.AssetManagerService.GetFontTtf("DefaultFont", LabelFontSize);

        if (_labelFont == null)
        {
            return;
        }

        var halfSize = 0.5f;
        foreach (var side in Enum.GetValues<SideType>())
        {
            var worldCenter = Vector3.Transform(GetFaceCenter(side, halfSize), _lastWorld);
            var projected = _graphicsDevice.Viewport.Project(worldCenter, _lastProjection, _lastView, Matrix.Identity);

            if (projected.Z < 0 || projected.Z > 1)
            {
                continue;
            }

            var text = side.ToString();
            var textSize = _labelFont.MeasureString(text);
            var drawPosition = new Vector2(projected.X - textSize.X / 2f, projected.Y - textSize.Y / 2f);

            spriteBatch.DrawString(_labelFont, text, drawPosition, LabelColor);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _effect.Dispose();
    }

    private void EnsureGeometry()
    {
        if (!_geometryInvalidated)
        {
            return;
        }

        var halfSize = 0.5f;
        var vertices = new List<VertexPositionTexture>(24);
        var indices = new List<short>(36);
        short baseIndex = 0;

        Texture2D? atlasTexture = null;

        foreach (var side in Enum.GetValues<SideType>())
        {
            var region = _blockManagerService.GetBlockSide(BlockType, side);

            if (region == null)
            {
                _logger.Warning("Texture region for block {BlockType} side {SideType} not found", BlockType, side);
                continue;
            }

            atlasTexture ??= region.Texture;

            var uv = ExtractUv(region);
            var faceVertices = GetFaceVertices(side, halfSize, uv);

            vertices.AddRange(faceVertices);
            indices.AddRange(new[]
            {
                baseIndex,
                (short)(baseIndex + 1),
                (short)(baseIndex + 2),
                (short)(baseIndex + 2),
                (short)(baseIndex + 3),
                baseIndex
            });

            baseIndex += 4;
        }

        if (vertices.Count == 0 || indices.Count == 0 || atlasTexture == null)
        {
            _logger.Warning("Unable to build geometry for block {BlockType}; missing texture data", BlockType);
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _vertexBuffer = null;
            _indexBuffer = null;
            _currentAtlasTexture = null;
            _geometryInvalidated = false;
            return;
        }

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionTexture), vertices.Count, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices.ToArray());

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices.ToArray());

        _currentAtlasTexture = atlasTexture;
        _geometryInvalidated = false;
    }

    private static VertexPositionTexture[] GetFaceVertices(SideType side, float halfSize, (Vector2 Min, Vector2 Max) uv)
    {
        var (min, max) = uv;

        return side switch
        {
            SideType.Top => new[]
            {
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, -halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, -halfSize), new Vector2(max.X, min.Y))
            },
            SideType.Bottom => new[]
            {
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, -halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, -halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, halfSize), new Vector2(max.X, min.Y))
            },
            SideType.North => new[]
            {
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, -halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, -halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, -halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, -halfSize), new Vector2(max.X, min.Y))
            },
            SideType.South => new[]
            {
                new VertexPositionTexture(new Vector3(halfSize, halfSize, halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, halfSize), new Vector2(max.X, min.Y))
            },
            SideType.East => new[]
            {
                new VertexPositionTexture(new Vector3(halfSize, halfSize, -halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, -halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, -halfSize, halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(halfSize, halfSize, halfSize), new Vector2(max.X, min.Y))
            },
            SideType.West => new[]
            {
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, halfSize), new Vector2(min.X, min.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, halfSize), new Vector2(min.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, -halfSize, -halfSize), new Vector2(max.X, max.Y)),
                new VertexPositionTexture(new Vector3(-halfSize, halfSize, -halfSize), new Vector2(max.X, min.Y))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Unsupported side type")
        };
    }

    private static Vector3 GetFaceCenter(SideType side, float halfSize)
    {
        return side switch
        {
            SideType.Top => new Vector3(0, halfSize, 0),
            SideType.Bottom => new Vector3(0, -halfSize, 0),
            SideType.North => new Vector3(0, 0, -halfSize),
            SideType.South => new Vector3(0, 0, halfSize),
            SideType.East => new Vector3(halfSize, 0, 0),
            SideType.West => new Vector3(-halfSize, 0, 0),
            _ => Vector3.Zero
        };
    }

    private static (Vector2 Min, Vector2 Max) ExtractUv(Texture2DRegion region)
    {
        var texture = region.Texture;
        var bounds = region.Bounds;

        var min = new Vector2(bounds.X / (float)texture.Width, bounds.Y / (float)texture.Height);
        var max = new Vector2((bounds.X + bounds.Width) / (float)texture.Width, (bounds.Y + bounds.Height) / (float)texture.Height);

        return (min, max);
    }
}
