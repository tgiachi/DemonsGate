using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SquidCraft.Client.Components.Base;

/// <summary>
/// Base class for UI components with positioning, sizing, and rendering support
/// </summary>
public abstract class BaseComponent : IDisposable
{
    private Vector2 _position;
    private Vector2 _size;
    private float _rotation;
    private float _opacity = 1.0f;
    private bool _isVisible = true;
    private bool _isDisposed;

    /// <summary>
    /// Gets or sets the relative position of the component
    /// </summary>
    public Vector2 Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                OnPositionChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the size of the component
    /// </summary>
    public Vector2 Size
    {
        get => _size;
        set
        {
            if (_size != value)
            {
                _size = value;
                OnSizeChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the rotation angle in radians
    /// </summary>
    public float Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation != value)
            {
                _rotation = value;
                OnRotationChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the opacity (0.0 to 1.0)
    /// </summary>
    public float Opacity
    {
        get => _opacity;
        set
        {
            _opacity = MathHelper.Clamp(value, 0.0f, 1.0f);
            OnOpacityChanged();
        }
    }

    /// <summary>
    /// Gets or sets whether the component is visible
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnVisibilityChanged();
            }
        }
    }

    /// <summary>
    /// Gets the absolute position in screen coordinates
    /// </summary>
    public virtual Vector2 AbsolutePosition => Position;

    /// <summary>
    /// Gets the bounds rectangle of the component
    /// </summary>
    public Rectangle Bounds => new((int)AbsolutePosition.X, (int)AbsolutePosition.Y, (int)Size.X, (int)Size.Y);

    /// <summary>
    /// Initializes the component
    /// </summary>
    public virtual void Initialize()
    {
    }

    /// <summary>
    /// Updates the component
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public virtual void Update(GameTime gameTime)
    {
    }

    /// <summary>
    /// Draws the component
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="parentPosition">Parent position offset</param>
    public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition = default)
    {
        if (!IsVisible || _isDisposed)
        {
            return;
        }

        DrawContent(spriteBatch, gameTime, parentPosition);
    }

    /// <summary>
    /// Override this method to provide custom drawing logic
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="parentPosition">Parent position offset</param>
    protected virtual void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
    }

    /// <summary>
    /// Checks if a point is within the component's bounds
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <returns>True if point is within bounds</returns>
    public bool Contains(Vector2 point)
    {
        var bounds = Bounds;
        return point.X >= bounds.X && point.X < bounds.Right &&
               point.Y >= bounds.Y && point.Y < bounds.Bottom;
    }

    /// <summary>
    /// Called when position changes
    /// </summary>
    protected virtual void OnPositionChanged()
    {
    }

    /// <summary>
    /// Called when size changes
    /// </summary>
    protected virtual void OnSizeChanged()
    {
    }

    /// <summary>
    /// Called when rotation changes
    /// </summary>
    protected virtual void OnRotationChanged()
    {
    }

    /// <summary>
    /// Called when opacity changes
    /// </summary>
    protected virtual void OnOpacityChanged()
    {
    }

    /// <summary>
    /// Called when visibility changes
    /// </summary>
    protected virtual void OnVisibilityChanged()
    {
    }

    /// <summary>
    /// Disposes the component
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override this method to dispose managed resources
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}
