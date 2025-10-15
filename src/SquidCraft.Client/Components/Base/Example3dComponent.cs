using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.Base;

/// <summary>
/// Example 3D component demonstrating Base3dComponent usage
/// </summary>
public class Example3dComponent : Base3dComponent
{
    private BasicEffect? _effect;
    private VertexPositionColor[] _vertices;
    private short[] _indices;

    public Example3dComponent()
    {
        Name = "Example 3D Cube";

        // Create a simple colored cube
        CreateCubeGeometry();
    }

    public override void Initialize()
    {
        base.Initialize();

        _effect = new BasicEffect(SquidCraftClientContext.GraphicsDevice)
        {
            VertexColorEnabled = true,
            Projection = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45),
                SquidCraftClientContext.GraphicsDevice.Viewport.AspectRatio,
                0.1f, 1000f)
        };
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Rotate the cube over time
        Rotation += new Vector3(0.01f, 0.005f, 0.002f);
    }

    public override void Draw3d(GameTime gameTime)
    {
        if (_effect == null || !IsVisible)
            return;

        var graphicsDevice = SquidCraftClientContext.GraphicsDevice;

        // Set up the effect
        _effect.World = GetWorldMatrix();
        _effect.View = Matrix.CreateLookAt(
            new Vector3(0, 0, 10), // Camera position
            Vector3.Zero,           // Look at target
            Vector3.Up);            // Up vector

        // Draw the cube
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _vertices, 0, _vertices.Length,
                _indices, 0, _indices.Length / 3);
        }
    }

    private void CreateCubeGeometry()
    {
        // Define cube vertices (position + color)
        _vertices = new VertexPositionColor[]
        {
            // Front face
            new(new Vector3(-1, -1, 1), Color.Red),
            new(new Vector3(1, -1, 1), Color.Red),
            new(new Vector3(1, 1, 1), Color.Red),
            new(new Vector3(-1, 1, 1), Color.Red),

            // Back face
            new(new Vector3(-1, -1, -1), Color.Blue),
            new(new Vector3(-1, 1, -1), Color.Blue),
            new(new Vector3(1, 1, -1), Color.Blue),
            new(new Vector3(1, -1, -1), Color.Blue),

            // Left face
            new(new Vector3(-1, -1, -1), Color.Green),
            new(new Vector3(-1, 1, -1), Color.Green),
            new(new Vector3(-1, 1, 1), Color.Green),
            new(new Vector3(-1, -1, 1), Color.Green),

            // Right face
            new(new Vector3(1, -1, 1), Color.Yellow),
            new(new Vector3(1, 1, 1), Color.Yellow),
            new(new Vector3(1, 1, -1), Color.Yellow),
            new(new Vector3(1, -1, -1), Color.Yellow),

            // Top face
            new(new Vector3(-1, 1, 1), Color.Purple),
            new(new Vector3(1, 1, 1), Color.Purple),
            new(new Vector3(1, 1, -1), Color.Purple),
            new(new Vector3(-1, 1, -1), Color.Purple),

            // Bottom face
            new(new Vector3(-1, -1, -1), Color.Cyan),
            new(new Vector3(1, -1, -1), Color.Cyan),
            new(new Vector3(1, -1, 1), Color.Cyan),
            new(new Vector3(-1, -1, 1), Color.Cyan)
        };

        // Define cube indices (triangles)
        _indices = new short[]
        {
            // Front face
            0, 1, 2, 0, 2, 3,
            // Back face
            4, 5, 6, 4, 6, 7,
            // Left face
            8, 9, 10, 8, 10, 11,
            // Right face
            12, 13, 14, 12, 14, 15,
            // Top face
            16, 17, 18, 16, 18, 19,
            // Bottom face
            20, 21, 22, 20, 22, 23
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _effect?.Dispose();
        }
        base.Dispose(disposing);
    }
}
