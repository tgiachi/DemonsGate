using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Components;

public sealed class BlockOutlineComponent : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _effect;
    private VertexBuffer? _vertexBuffer;
    private bool _isDisposed;

    public BlockOutlineComponent(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        
        _effect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            TextureEnabled = false
        };

        CreateOutlineGeometry();
    }

    public Color OutlineColor { get; set; } = Color.White;

    public float LineWidth { get; set; } = 2f;

    public void Draw(Vector3 blockPosition, Matrix view, Matrix projection)
    {
        if (_vertexBuffer == null)
        {
            return;
        }

        var world = Matrix.CreateTranslation(blockPosition);

        _effect.World = world;
        _effect.View = view;
        _effect.Projection = projection;

        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        var previousRasterizerState = _graphicsDevice.RasterizerState;

        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, 12);
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.DepthStencilState = previousDepthStencilState;
        _graphicsDevice.RasterizerState = previousRasterizerState;
    }

    private void CreateOutlineGeometry()
    {
        const float offset = 0.005f;
        var min = new Vector3(-offset, -offset, -offset);
        var max = new Vector3(1f + offset, 1f + offset, 1f + offset);

        var vertices = new VertexPositionColor[]
        {
            new(new Vector3(min.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, min.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, min.Y, max.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, min.Y, max.Z), OutlineColor),

            new(new Vector3(min.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, min.Y, min.Z), OutlineColor),

            new(new Vector3(min.X, max.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, max.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, max.Z), OutlineColor),

            new(new Vector3(max.X, max.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, max.Z), OutlineColor),

            new(new Vector3(min.X, max.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(min.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, min.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, min.Z), OutlineColor),

            new(new Vector3(max.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(max.X, max.Y, max.Z), OutlineColor),

            new(new Vector3(min.X, min.Y, max.Z), OutlineColor),
            new(new Vector3(min.X, max.Y, max.Z), OutlineColor),
        };

        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _vertexBuffer?.Dispose();
        _effect.Dispose();
    }
}
