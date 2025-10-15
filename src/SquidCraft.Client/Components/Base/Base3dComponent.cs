using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Components.Base;

/// <summary>
/// Base class for 3D components with positioning, rotation, scaling, and rendering support
/// </summary>
public abstract class Base3dComponent : ISC3dDrawableComponent, ISCInitializable, IDisposable
{
    private Vector3 _position;
    private Vector3 _rotation;
    private Vector3 _scale = Vector3.One;
    private bool _isVisible = true;
    private bool _isEnabled = true;
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
    /// Gets or sets the position of the component in 3D space
    /// </summary>
    public Vector3 Position
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
    /// Gets or sets the rotation of the component in radians (Yaw, Pitch, Roll)
    /// </summary>
    public Vector3 Rotation
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
    /// Gets or sets the scale of the component
    /// </summary>
    public Vector3 Scale
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
    /// Gets or sets the opacity of the component (0.0 to 1.0)
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether this component has input focus
    /// </summary>
    public bool HasFocus { get; set; }

    /// <summary>
    /// Initializes the component
    /// </summary>
    public virtual void Initialize()
    {
    }

    /// <summary>
    /// Updates the component logic
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public virtual void Update(GameTime gameTime)
    {
    }

    /// <summary>
    /// Draws the 3D component
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public abstract void Draw3d(GameTime gameTime);

    /// <summary>
    /// Handles keyboard input when the component has focus
    /// </summary>
    /// <param name="keyboardState">Current keyboard state</param>
    /// <param name="gameTime">Game timing information</param>
    public virtual void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
    }

    /// <summary>
    /// Handles mouse input when the component has focus
    /// </summary>
    /// <param name="mouseState">Current mouse state</param>
    /// <param name="gameTime">Game timing information</param>
    public virtual void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
    }

    /// <summary>
    /// Gets the world transformation matrix for this component
    /// </summary>
    /// <returns>World matrix combining scale, rotation, and translation</returns>
    public Matrix GetWorldMatrix()
    {
        return Matrix.CreateScale(Scale) *
               Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
               Matrix.CreateTranslation(Position);
    }

    /// <summary>
    /// Called when the position changes
    /// </summary>
    protected virtual void OnPositionChanged()
    {
    }

    /// <summary>
    /// Called when the rotation changes
    /// </summary>
    protected virtual void OnRotationChanged()
    {
    }

    /// <summary>
    /// Called when the scale changes
    /// </summary>
    protected virtual void OnScaleChanged()
    {
    }

    /// <summary>
    /// Called when the visibility changes
    /// </summary>
    protected virtual void OnVisibilityChanged()
    {
    }

    /// <summary>
    /// Called when the enabled state changes
    /// </summary>
    protected virtual void OnEnabledChanged()
    {
    }

    /// <summary>
    /// Disposes the component and releases resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the component
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // Dispose managed resources here
            }
            _isDisposed = true;
        }
    }
}
