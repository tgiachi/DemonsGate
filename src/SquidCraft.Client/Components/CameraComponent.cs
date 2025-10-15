using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Game.Data.Primitives;

namespace SquidCraft.Client.Components;

public sealed class CameraComponent
{
    private Vector3 _position;
    private float _fieldOfView = MathHelper.PiOver4;
    private float _nearPlane = 0.1f;
    private float _farPlane = 1000f;
    
    private readonly Vector3 _worldUp = Vector3.Up;
    private Vector3 _front;
    private Vector3 _right;
    private Vector3 _up;
    
    private Matrix _view;
    private Matrix _projection;
    private bool _viewDirty = true;
    private bool _projectionDirty = true;
    
    private readonly GraphicsDevice _graphicsDevice;

    private float _yaw = -90f;
    private float _pitch;
    private float _zoom = 60f;
    private Point _lastMousePosition;
    private bool _firstMouseMove = true;

    public CameraComponent(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _position = new Vector3(8f, ChunkEntity.Size + 2f, 8f);
        
        _front = Vector3.UnitZ;
        _up = Vector3.Up;
        _right = Vector3.Normalize(Vector3.Cross(_front, _worldUp));
        
        UpdateCameraVectors();
    }

    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                _viewDirty = true;
            }
        }
    }

    public Vector3 Front => _front;
    
    public Vector3 Right => _right;
    
    public Vector3 Up => _up;
    
    public float Yaw
    {
        get => _yaw;
        set
        {
            _yaw = value;
            UpdateCameraVectors();
        }
    }
    
    public float Pitch
    {
        get => _pitch;
        set
        {
            _pitch = MathHelper.Clamp(value, -89f, 89f);
            UpdateCameraVectors();
        }
    }
    
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = MathHelper.Clamp(value, 1f, 120f);
            _fieldOfView = MathHelper.ToRadians(_zoom);
            _projectionDirty = true;
        }
    }

    public float FieldOfView
    {
        get => _fieldOfView;
        set
        {
            if (Math.Abs(_fieldOfView - value) > float.Epsilon)
            {
                _fieldOfView = value;
                _projectionDirty = true;
            }
        }
    }

    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            if (Math.Abs(_nearPlane - value) > float.Epsilon)
            {
                _nearPlane = value;
                _projectionDirty = true;
            }
        }
    }

    public float FarPlane
    {
        get => _farPlane;
        set
        {
            if (Math.Abs(_farPlane - value) > float.Epsilon)
            {
                _farPlane = value;
                _projectionDirty = true;
            }
        }
    }

    public Matrix View
    {
        get
        {
            if (_viewDirty)
            {
                _view = Matrix.CreateLookAt(_position, _position + _front, _up);
                _viewDirty = false;
            }
            return _view;
        }
    }

    public Matrix Projection
    {
        get
        {
            if (_projectionDirty)
            {
                var viewport = _graphicsDevice.Viewport;
                var aspectRatio = viewport.AspectRatio <= 0 ? 1f : viewport.AspectRatio;
                _projection = Matrix.CreatePerspectiveFieldOfView(_fieldOfView, aspectRatio, _nearPlane, _farPlane);
                _projectionDirty = false;
            }
            return _projection;
        }
    }

    public void Move(Vector3 delta)
    {
        _position += delta;
        _viewDirty = true;
    }

    public void ModifyDirection(float xOffset, float yOffset)
    {
        _yaw += xOffset;
        _pitch -= yOffset;
        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
        
        UpdateCameraVectors();
    }

    public void ModifyZoom(float zoomAmount)
    {
        Zoom = MathHelper.Clamp(_zoom - zoomAmount, 1f, 120f);
    }

    private void UpdateCameraVectors()
    {
        var yawRadians = MathHelper.ToRadians(_yaw);
        var pitchRadians = MathHelper.ToRadians(_pitch);
        
        var cameraDirection = new Vector3(
            MathF.Cos(yawRadians) * MathF.Cos(pitchRadians),
            MathF.Sin(pitchRadians),
            MathF.Sin(yawRadians) * MathF.Cos(pitchRadians)
        );
        
        _front = Vector3.Normalize(cameraDirection);
        _right = Vector3.Normalize(Vector3.Cross(_front, _worldUp));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        
        _viewDirty = true;
    }

    public Ray GetPickRay()
    {
        return new Ray(_position, _front);
    }

    public Ray GetPickRay(int screenX, int screenY)
    {
        var viewport = _graphicsDevice.Viewport;
        
        var nearPoint = viewport.Unproject(
            new Vector3(screenX, screenY, 0f),
            Projection,
            View,
            Matrix.Identity
        );
        
        var farPoint = viewport.Unproject(
            new Vector3(screenX, screenY, 1f),
            Projection,
            View,
            Matrix.Identity
        );
        
        var direction = farPoint - nearPoint;
        direction.Normalize();
        
        return new Ray(nearPoint, direction);
    }

    public float MoveSpeed { get; set; } = 20f;

    public float MouseSensitivity { get; set; } = 0.003f;

    public bool EnableInput { get; set; } = true;

    public void Update(GameTime gameTime)
    {
        if (!EnableInput)
        {
            return;
        }

        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        HandleKeyboardInput(deltaTime);
        HandleMouseInput();
    }

    private void HandleKeyboardInput(float deltaTime)
    {
        var keyboardState = Keyboard.GetState();
        var moveDistance = MoveSpeed * deltaTime;

        var movement = Vector3.Zero;

        if (keyboardState.IsKeyDown(Keys.W))
        {
            movement += _front;
        }
        if (keyboardState.IsKeyDown(Keys.S))
        {
            movement -= _front;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            movement -= _right;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            movement += _right;
        }
        if (keyboardState.IsKeyDown(Keys.Space))
        {
            movement += _worldUp;
        }
        if (keyboardState.IsKeyDown(Keys.LeftShift))
        {
            movement -= _worldUp;
        }

        if (movement != Vector3.Zero)
        {
            movement.Normalize();
            movement *= moveDistance;
            Move(movement);
        }
    }

    private void HandleMouseInput()
    {
        var mouseState = Mouse.GetState();
        var currentMousePosition = new Point(mouseState.X, mouseState.Y);

        if (_firstMouseMove)
        {
            _lastMousePosition = currentMousePosition;
            _firstMouseMove = false;
            return;
        }

        var deltaX = currentMousePosition.X - _lastMousePosition.X;
        var deltaY = currentMousePosition.Y - _lastMousePosition.Y;

        if (deltaX != 0 || deltaY != 0)
        {
            var xOffset = deltaX * MouseSensitivity;
            var yOffset = deltaY * MouseSensitivity;
            
            ModifyDirection(xOffset, yOffset);
        }

        _lastMousePosition = currentMousePosition;
    }
}
