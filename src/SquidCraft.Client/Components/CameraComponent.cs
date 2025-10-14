using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SquidCraft.Client.Components;

public sealed class CameraComponent
{
    private Vector3 _position;
    private Vector3 _target;
    private float _fieldOfView = MathHelper.PiOver4;
    private float _nearPlane = 0.1f;
    private float _farPlane = 500f;
    private Vector3 _up = Vector3.Up;
    
    private Matrix _view;
    private Matrix _projection;
    private bool _viewDirty = true;
    private bool _projectionDirty = true;
    
    private readonly GraphicsDevice _graphicsDevice;

    private float _yaw;
    private float _pitch;
    private Point _lastMousePosition;
    private bool _firstMouseMove = true;

    public CameraComponent(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _position = new Vector3(55f, 65f, 55f);
        _target = new Vector3(8f, 18f, 8f);
        
        var direction = _target - _position;
        direction.Normalize();
        _yaw = MathF.Atan2(direction.X, direction.Z);
        _pitch = MathF.Asin(-direction.Y);
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

    public Vector3 Target
    {
        get => _target;
        set
        {
            if (_target != value)
            {
                _target = value;
                _viewDirty = true;
            }
        }
    }

    public Vector3 Up
    {
        get => _up;
        set
        {
            if (_up != value)
            {
                _up = value;
                _viewDirty = true;
            }
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
                _view = Matrix.CreateLookAt(_position, _target, _up);
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

    public void LookAt(Vector3 position, Vector3 target)
    {
        _position = position;
        _target = target;
        _viewDirty = true;
    }

    public void LookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        _position = position;
        _target = target;
        _up = up;
        _viewDirty = true;
    }

    public void Move(Vector3 delta)
    {
        _position += delta;
        _target += delta;
        _viewDirty = true;
    }

    public void Rotate(float yaw, float pitch)
    {
        var direction = _target - _position;
        var distance = direction.Length();
        
        direction.Normalize();
        
        var rotationMatrix = Matrix.CreateFromYawPitchRoll(yaw, pitch, 0);
        direction = Vector3.Transform(direction, rotationMatrix);
        
        _target = _position + direction * distance;
        _viewDirty = true;
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

        var forward = _target - _position;
        forward.Normalize();

        var right = Vector3.Cross(forward, _up);
        right.Normalize();

        var movement = Vector3.Zero;

        if (keyboardState.IsKeyDown(Keys.W))
        {
            movement += forward;
        }
        if (keyboardState.IsKeyDown(Keys.S))
        {
            movement -= forward;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            movement -= right;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            movement += right;
        }
        if (keyboardState.IsKeyDown(Keys.Space))
        {
            movement += _up;
        }
        if (keyboardState.IsKeyDown(Keys.LeftShift))
        {
            movement -= _up;
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
            _yaw += deltaX * MouseSensitivity;
            _pitch -= deltaY * MouseSensitivity;

            _pitch = MathHelper.Clamp(_pitch, -MathHelper.PiOver2 + 0.01f, MathHelper.PiOver2 - 0.01f);

            var forward = new Vector3(
                MathF.Sin(_yaw) * MathF.Cos(_pitch),
                -MathF.Sin(_pitch),
                MathF.Cos(_yaw) * MathF.Cos(_pitch)
            );

            _target = _position + forward;
            _viewDirty = true;
        }

        _lastMousePosition = currentMousePosition;
    }
}
