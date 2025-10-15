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

    private Vector3 _velocity = Vector3.Zero;
    private bool _isOnGround;
    private bool _enablePhysics = true;

    public CameraComponent(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _position = new Vector3(8f, ChunkEntity.Height + 20f, 8f);
        
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

    public bool EnablePhysics
    {
        get => _enablePhysics;
        set => _enablePhysics = value;
    }

    public bool FlyMode { get; set; } = false;

    public float Gravity { get; set; } = 32f;

    public float JumpForce { get; set; } = 10f;

    public Vector3 BoundingBoxSize { get; set; } = new Vector3(0.6f, 1.8f, 0.6f);

    public bool IsOnGround => _isOnGround;

    public Func<Vector3, Vector3, bool>? CheckCollision { get; set; }

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (EnablePhysics && !FlyMode)
        {
            ApplyPhysics(deltaTime);
        }

        if (EnableInput)
        {
            HandleKeyboardInput(deltaTime);
            HandleMouseInput();
        }
    }

    private void ApplyPhysics(float deltaTime)
    {
        _velocity.Y -= Gravity * deltaTime;

        var newPosition = _position + _velocity * deltaTime;

        if (CheckCollision != null)
        {
            newPosition = ResolveCollisions(newPosition);
        }

        _position = newPosition;
        _viewDirty = true;
    }

    private Vector3 ResolveCollisions(Vector3 newPosition)
    {
        var halfSize = BoundingBoxSize * 0.5f;
        
        _isOnGround = false;

        var resolved = newPosition;

        if (_velocity.Y <= 0)
        {
            var feetY = newPosition.Y - BoundingBoxSize.Y * 0.5f;
            var blockY = (int)MathF.Floor(feetY);

            var minX = newPosition.X - halfSize.X;
            var maxX = newPosition.X + halfSize.X;
            var minZ = newPosition.Z - halfSize.Z;
            var maxZ = newPosition.Z + halfSize.Z;

            for (float x = minX; x <= maxX; x += 0.3f)
            {
                for (float z = minZ; z <= maxZ; z += 0.3f)
                {
                    var testPos = new Vector3(x, blockY, z);
                    
                    if (CheckCollision!(testPos, Vector3.Zero) == true)
                    {
                        resolved.Y = blockY + 1.0f + BoundingBoxSize.Y * 0.5f;
                        _velocity.Y = 0;
                        _isOnGround = true;
                        break;
                    }
                }
                if (_isOnGround) break;
            }
        }

        return resolved;
    }

    private void HandleKeyboardInput(float deltaTime)
    {
        var keyboardState = Keyboard.GetState();

        if (EnablePhysics && !FlyMode)
        {
            var horizontalMovement = Vector3.Zero;

            var forwardFlat = new Vector3(_front.X, 0, _front.Z);
            if (forwardFlat != Vector3.Zero)
            {
                forwardFlat.Normalize();
            }

            var rightFlat = new Vector3(_right.X, 0, _right.Z);
            if (rightFlat != Vector3.Zero)
            {
                rightFlat.Normalize();
            }

            if (keyboardState.IsKeyDown(Keys.W))
            {
                horizontalMovement += forwardFlat;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                horizontalMovement -= forwardFlat;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                horizontalMovement -= rightFlat;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                horizontalMovement += rightFlat;
            }

            if (horizontalMovement != Vector3.Zero)
            {
                horizontalMovement.Normalize();
                horizontalMovement *= MoveSpeed * deltaTime;
                Move(horizontalMovement);
            }

            if (keyboardState.IsKeyDown(Keys.Space) && _isOnGround)
            {
                _velocity.Y = JumpForce;
            }
        }
        else
        {
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
