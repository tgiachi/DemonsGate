using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Collections;
using SquidCraft.Client.Components.Interfaces;

namespace SquidCraft.Client.Components.Base;

/// <summary>
/// Base class for UI components with positioning, sizing, and rendering support
/// </summary>
public abstract class BaseComponent : ISCDrawableComponent, IParentAwareComponent, IDisposable
{
    private readonly SCDrawableCollection<ISCDrawableComponent> _children = new();
    private Vector2 _position;
    private Vector2 _size;
    private Vector2 _scale = Vector2.One;
    private float _rotation;
    private float _opacity = 1.0f;
    private bool _isVisible = true;
    private bool _isEnabled = true;
    private bool _isFocused;
    private bool _hasFocus;
    private bool _isDisposed;

    /// <summary>
    /// Gets the unique identifier of the component
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the component
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the parent component
    /// </summary>
    public ISCDrawableComponent? Parent { get; private set; }

    /// <summary>
    /// Gets the children components
    /// </summary>
    public IEnumerable<ISCDrawableComponent> Children => _children;

    /// <summary>
    /// Gets or sets the Z-index for rendering order
    /// </summary>
    public int ZIndex { get; set; }

    /// <summary>
    /// Gets or sets whether the component is enabled
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnEnabledChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the component has focus
    /// </summary>
    public bool IsFocused
    {
        get => _isFocused;
        set
        {
            if (_isFocused != value)
            {
                _isFocused = value;
                OnFocusChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this component has input focus for keyboard and mouse events
    /// </summary>
    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (_hasFocus != value)
            {
                _hasFocus = value;
                OnInputFocusChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the scale of the component
    /// </summary>
    public Vector2 Scale
    {
        get => _scale;
        set
        {
            if (_scale != value)
            {
                _scale = value;
                OnScaleChanged();
            }
        }
    }

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
    /// Gets the size of the component
    /// </summary>
    public virtual Vector2 Size
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
        if (!IsEnabled)
        {
            return;
        }

        // Update children
        foreach (var child in _children)
        {
            child.Update(gameTime);
        }
    }

    /// <summary>
    /// Handles keyboard input when the component has focus
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gameTime">Game timing information</param>
    public virtual void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus)
        {
            return;
        }

        // Propagate input to children that have focus
        foreach (var child in _children)
        {
            if (child.HasFocus)
            {
                child.HandleKeyboard(keyboardState, gameTime);
            }
        }
    }

    /// <summary>
    /// Handles mouse input when the component has focus
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="gameTime">Game timing information</param>
    public virtual void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus)
        {
            return;
        }

        // Propagate input to children that have focus
        foreach (var child in _children)
        {
            if (child.HasFocus)
            {
                child.HandleMouse(mouseState, gameTime);
            }
        }
    }

    /// <summary>
    /// Draws the component (ISCDrawable implementation)
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Draw(spriteBatch, gameTime, Vector2.Zero);
    }

    /// <summary>
    /// Draws the component with parent position
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

        // Draw children
        foreach (var child in _children)
        {
            child.Draw(gameTime, spriteBatch);
        }
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
    /// Called when enabled state changes
    /// </summary>
    protected virtual void OnEnabledChanged()
    {
    }

    /// <summary>
    /// Called when focus state changes
    /// </summary>
    protected virtual void OnFocusChanged()
    {
    }

    /// <summary>
    /// Called when scale changes
    /// </summary>
    protected virtual void OnScaleChanged()
    {
    }

    /// <summary>
    /// Called when input focus changes
    /// </summary>
    protected virtual void OnInputFocusChanged()
    {
    }

    /// <summary>
    /// Adds a child component
    /// </summary>
    /// <param name="child">The child component to add</param>
    public void AddChild(ISCDrawableComponent child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (_children.Contains(child))
        {
            return;
        }

        if (child.Parent is BaseComponent existingParent && !ReferenceEquals(existingParent, this))
        {
            existingParent.RemoveChild(child);
        }

        _children.Add(child);

        if (child is IParentAwareComponent parentAware)
        {
            parentAware.SetParent(this);
        }
    }

    /// <summary>
    /// Removes a child component
    /// </summary>
    /// <param name="child">The child component to remove</param>
    public void RemoveChild(ISCDrawableComponent child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!_children.Remove(child))
        {
            return;
        }

        if (child is IParentAwareComponent parentAware)
        {
            parentAware.SetParent(null);
        }
    }

    /// <summary>
    /// Removes all child components
    /// </summary>
    public void ClearChildren()
    {
        var count = _children.Count;

        if (count == 0)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            if (_children[i] is IParentAwareComponent parentAware)
            {
                parentAware.SetParent(null);
            }
        }

        _children.Clear();
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

    void IParentAwareComponent.SetParent(ISCDrawableComponent? parent)
    {
        Parent = parent;
    }
}
