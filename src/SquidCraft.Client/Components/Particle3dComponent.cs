using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using Serilog;

namespace SquidCraft.Client.Components;

public class Particle
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float LifeTime;
    public float MaxLifeTime;
    public Color Color;
    public float Size;
    public bool IsActive;
    public Vector3 Rotation;
    public Vector3 RotationSpeed;
    private const float Gravity = 15f;

    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        Velocity.Y -= Gravity * deltaTime;
        Position += Velocity * deltaTime;
        Rotation += RotationSpeed * deltaTime;
        LifeTime -= deltaTime;

        if (LifeTime <= 0)
        {
            IsActive = false;
        }
    }

    public float GetAlpha()
    {
        return MathHelper.Clamp(LifeTime / MaxLifeTime, 0f, 1f);
    }

    public void Reset(Vector3 position, Vector3 velocity, float lifeTime, Color color, float size, Vector3 rotationSpeed)
    {
        Position = position;
        Velocity = velocity;
        LifeTime = lifeTime;
        MaxLifeTime = lifeTime;
        Color = color;
        Size = size;
        IsActive = true;
        Rotation = Vector3.Zero;
        RotationSpeed = rotationSpeed;
    }
}

public class ParticlePool
{
    private readonly Stack<Particle> _pool = new();
    private readonly List<Particle> _activeParticles = new();

    public int PoolSize { get; private set; }

    public void Initialize(int size)
    {
        PoolSize = size;
        for (int i = 0; i < size; i++)
        {
            _pool.Push(new Particle());
        }
    }

    public Particle? GetParticle()
    {
        if (_pool.Count > 0)
        {
            var particle = _pool.Pop();
            _activeParticles.Add(particle);
            return particle;
        }
        return null;
    }

    public void ReturnParticle(Particle particle)
    {
        particle.IsActive = false;
        _activeParticles.Remove(particle);
        _pool.Push(particle);
    }

    public IEnumerable<Particle> GetActiveParticles() => _activeParticles;

    public void Update(float deltaTime)
    {
        for (int i = _activeParticles.Count - 1; i >= 0; i--)
        {
            var particle = _activeParticles[i];
            particle.Update(deltaTime);
            if (!particle.IsActive)
            {
                ReturnParticle(particle);
            }
        }
    }

    public void Clear()
    {
        foreach (var particle in _activeParticles)
        {
            particle.IsActive = false;
            _pool.Push(particle);
        }
        _activeParticles.Clear();
    }
}

public class Particle3dComponent : Base3dComponent
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ParticlePool _particlePool = new();
    private BasicEffect _effect;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Texture2D _particleTexture;

    private const int MaxParticles = 1000;

    // Camera matrices for rendering
    public Matrix View { get; set; } = Matrix.CreateLookAt(new Vector3(0, 0, 10), Vector3.Zero, Vector3.Up);
    public Matrix Projection { get; set; }

    public Particle3dComponent()
    {
        _graphicsDevice = SquidCraftClientContext.GraphicsDevice;
        _effect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = true,
            LightingEnabled = false,
            VertexColorEnabled = true
        };

        _particlePool.Initialize(MaxParticles);
        CreateGeometry();
        LoadTexture();

        // Default projection
        var viewport = _graphicsDevice.Viewport;
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, viewport.AspectRatio, 0.1f, 1000f);
    }

    private void CreateGeometry()
    {
        // Create a quad for particles
        var vertices = new VertexPositionColorTexture[]
        {
            new(new Vector3(-0.5f, -0.5f, 0), Color.White, new Vector2(0, 1)),
            new(new Vector3(0.5f, -0.5f, 0), Color.White, new Vector2(1, 1)),
            new(new Vector3(0.5f, 0.5f, 0), Color.White, new Vector2(0, 0)),
            new(new Vector3(-0.5f, 0.5f, 0), Color.White, new Vector2(1, 0))
        };

        var indices = new short[] { 0, 1, 2, 2, 3, 0 };

        _vertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _indexBuffer.SetData(indices);
    }

    private void LoadTexture()
    {
        // Create a simple white texture for particles
        _particleTexture = new Texture2D(_graphicsDevice, 1, 1);
        _particleTexture.SetData(new[] { Color.White });
    }

    public void SpawnParticles(Vector3 position, int count, float spread = 1f, float speed = 5f, float lifeTime = 2f, Color? color = null)
    {
        var c = color ?? Color.Yellow;
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var particle = _particlePool.GetParticle();
            if (particle == null) break;

            var offsetPos = position + new Vector3(
                (float)(random.NextDouble() - 0.5) * 0.8f,
                (float)(random.NextDouble() - 0.5) * 0.8f,
                (float)(random.NextDouble() - 0.5) * 0.8f
            );

            var velocity = new Vector3(
                (float)(random.NextDouble() - 0.5) * spread,
                (float)(random.NextDouble() * 0.5 + 0.3) * spread,
                (float)(random.NextDouble() - 0.5) * spread
            ) * speed;

            var rotationSpeed = new Vector3(
                (float)(random.NextDouble() - 0.5) * 10f,
                (float)(random.NextDouble() - 0.5) * 10f,
                (float)(random.NextDouble() - 0.5) * 10f
            );

            particle.Reset(offsetPos, velocity, lifeTime, c, 0.15f, rotationSpeed);
        }
    }

    public override void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _particlePool.Update(deltaTime);

        // Log active particle count occasionally
        var activeCount = _particlePool.GetActiveParticles().Count();
        if (activeCount > 0 && gameTime.TotalGameTime.TotalSeconds % 1 < 0.1) // Log every second
        {
            Serilog.Log.Information("Active particles: {Count}", activeCount);
        }

        base.Update(gameTime);
    }

    public override void Draw3d(GameTime gameTime)
    {
        var activeParticles = _particlePool.GetActiveParticles();
        var particleCount = activeParticles.Count();
        if (particleCount == 0) return;

        Log.Verbose("Drawing {Count} particles", particleCount);

        // Create dynamic vertex buffer for all particles
        var totalVertices = particleCount * 4; // 4 vertices per quad
        var totalIndices = particleCount * 6; // 6 indices per quad

        var vertices = new VertexPositionColorTexture[totalVertices];
        var indices = new short[totalIndices];

        int vertexIndex = 0;
        int indexIndex = 0;
        int particleIndex = 0;

        foreach (var particle in activeParticles)
        {
            // Base quad vertices (will be transformed)
            var baseVertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };

            var rotation = Matrix.CreateFromYawPitchRoll(particle.Rotation.Y, particle.Rotation.X, particle.Rotation.Z);
            var world = Matrix.CreateScale(particle.Size) * rotation * Matrix.CreateTranslation(particle.Position);

            var alpha = particle.GetAlpha();
            var colorWithAlpha = particle.Color * alpha;

            for (int i = 0; i < 4; i++)
            {
                var transformedPos = Vector3.Transform(baseVertices[i], world);
                vertices[vertexIndex + i] = new VertexPositionColorTexture(
                    transformedPos,
                    colorWithAlpha,
                    i switch
                    {
                        0 => new Vector2(0, 1),
                        1 => new Vector2(1, 1),
                        2 => new Vector2(1, 0),
                        3 => new Vector2(0, 0),
                        _ => Vector2.Zero
                    }
                );
            }

            // Indices for this quad
            var baseVertexIndex = (short)(vertexIndex);
            indices[indexIndex + 0] = (short)(baseVertexIndex + 0);
            indices[indexIndex + 1] = (short)(baseVertexIndex + 1);
            indices[indexIndex + 2] = (short)(baseVertexIndex + 2);
            indices[indexIndex + 3] = (short)(baseVertexIndex + 2);
            indices[indexIndex + 4] = (short)(baseVertexIndex + 3);
            indices[indexIndex + 5] = (short)(baseVertexIndex + 0);

            vertexIndex += 4;
            indexIndex += 6;
            particleIndex++;
        }

        // Create dynamic buffers
        var dynamicVertexBuffer = new DynamicVertexBuffer(_graphicsDevice, typeof(VertexPositionColorTexture), totalVertices, BufferUsage.WriteOnly);
        var dynamicIndexBuffer = new DynamicIndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, totalIndices, BufferUsage.WriteOnly);

        dynamicVertexBuffer.SetData(vertices);
        dynamicIndexBuffer.SetData(indices);

        _effect.View = View;
        _effect.Projection = Projection;
        _effect.Texture = _particleTexture;
        _effect.World = Matrix.Identity;

        var previousBlendState = _graphicsDevice.BlendState;
        var previousDepthStencilState = _graphicsDevice.DepthStencilState;
        
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        _graphicsDevice.SetVertexBuffer(dynamicVertexBuffer);
        _graphicsDevice.Indices = dynamicIndexBuffer;

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, particleCount * 2);
        }

        _graphicsDevice.SetVertexBuffer(null);
        _graphicsDevice.Indices = null;
        
        _graphicsDevice.BlendState = previousBlendState;
        _graphicsDevice.DepthStencilState = previousDepthStencilState;

        // Dispose dynamic buffers
        dynamicVertexBuffer.Dispose();
        dynamicIndexBuffer.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _particleTexture?.Dispose();
            _effect?.Dispose();
            _particlePool.Clear();
        }
        base.Dispose(disposing);
    }
}