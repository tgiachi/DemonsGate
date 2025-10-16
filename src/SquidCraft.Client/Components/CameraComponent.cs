using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Game.Data.Primitives;

namespace SquidCraft.Client.Components;

/// <summary>
/// Represents a first-person 3D camera component with input handling, physics, and collision detection.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraComponent"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for viewport calculations.</param>
    public CameraComponent(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _position = new Vector3(8f, ChunkEntity.Height + 20f, 8f);
        
        _front = Vector3.UnitZ;
        _up = Vector3.Up;
        _right = Vector3.Normalize(Vector3.Cross(_front, _worldUp));
        
        UpdateCameraVectors();
        
        Mouse.SetPosition(_graphicsDevice.Viewport.Width / 2, _graphicsDevice.Viewport.Height / 2);
    }

    /// <summary>
    /// Gets or sets the camera's world position.
    /// </summary>
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

    /// <summary>
    /// Gets the forward direction vector (normalized).
    /// </summary>
    public Vector3 Front => _front;
    
    /// <summary>
    /// Gets the right direction vector (normalized).
    /// </summary>
    public Vector3 Right => _right;
    
    /// <summary>
    /// Gets the up direction vector (normalized).
    /// </summary>
    public Vector3 Up => _up;
    
    /// <summary>
    /// Gets or sets the horizontal rotation in degrees (default: -90°).
    /// </summary>
    public float Yaw
    {
        get => _yaw;
        set
        {
            _yaw = value;
            UpdateCameraVectors();
        }
    }
    
    /// <summary>
    /// Gets or sets the vertical rotation in degrees (clamped: -89° to 89°).
    /// </summary>
    public float Pitch
    {
        get => _pitch;
        set
        {
            _pitch = MathHelper.Clamp(value, -89f, 89f);
            UpdateCameraVectors();
        }
    }
    
    /// <summary>
    /// Gets or sets the field of view in degrees (default: 60°, range: 1-120°).
    /// </summary>
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

    /// <summary>
    /// Gets or sets the field of view in radians.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the near clipping plane distance.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the far clipping plane distance.
    /// </summary>
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

    /// <summary>
    /// Gets the view matrix for the camera.
    /// </summary>
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

    /// <summary>
    /// Gets the projection matrix for the camera.
    /// </summary>
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

    /// <summary>
    /// Translates the camera by the specified delta vector.
    /// </summary>
    /// <param name="delta">The translation vector to add to the current position.</param>
    public void Move(Vector3 delta)
    {
        _position += delta;
        _viewDirty = true;
    }

    /// <summary>
    /// Modifies the camera's yaw and pitch based on mouse movement offsets.
    /// </summary>
    /// <param name="xOffset">The horizontal offset in degrees.</param>
    /// <param name="yOffset">The vertical offset in degrees.</param>
    public void ModifyDirection(float xOffset, float yOffset)
    {
        _yaw += xOffset;
        _pitch -= yOffset;
        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
        
        UpdateCameraVectors();
    }

    /// <summary>
    /// Adjusts the camera's field of view zoom.
    /// </summary>
    /// <param name="zoomAmount">The amount to zoom in (positive) or out (negative).</param>
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

    /// <summary>
    /// Gets a ray from the camera position in the forward direction for raycasting.
    /// </summary>
    /// <returns>A ray starting from the camera position in the forward direction.</returns>
    public Ray GetPickRay()
    {
        return new Ray(_position, _front);
    }

    /// <summary>
    /// Gets a ray from the camera through the specified screen coordinates for raycasting.
    /// </summary>
    /// <param name="screenX">The X coordinate on the screen.</param>
    /// <param name="screenY">The Y coordinate on the screen.</param>
    /// <returns>A ray from the camera through the screen point.</returns>
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

    /// <summary>
    /// Gets or sets the movement speed of the camera.
    /// </summary>
    public float MoveSpeed { get; set; } = 20f;

    /// <summary>
    /// Gets or sets the mouse look sensitivity.
    /// </summary>
    public float MouseSensitivity { get; set; } = 0.003f;

    /// <summary>
    /// Gets or sets a value indicating whether input handling is enabled.
    /// </summary>
    public bool EnableInput { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the mouse is captured for camera control.
    /// </summary>
    public bool IsMouseCaptured { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether physics (gravity and collisions) are enabled.
    /// </summary>
    public bool EnablePhysics
    {
        get => _enablePhysics;
        set => _enablePhysics = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether fly mode is enabled (disables physics for creative flight).
    /// </summary>
    public bool FlyMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the gravity acceleration.
    /// </summary>
    public float Gravity { get; set; } = 32f;

    /// <summary>
    /// Gets or sets the jump velocity.
    /// </summary>
    public float JumpForce { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the player collision box size.
    /// </summary>
    public Vector3 BoundingBoxSize { get; set; } = new Vector3(0.6f, 1.8f, 0.6f);

    /// <summary>
    /// Gets a value indicating whether the player is on solid ground.
    /// </summary>
    public bool IsOnGround => _isOnGround;

    /// <summary>
    /// Gets or sets the delegate for world collision testing.
    /// </summary>
    public Func<Vector3, Vector3, bool>? CheckCollision { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether collision debugging is enabled.
    /// </summary>
    public bool EnableCollisionDebug { get; set; } = false;
    
    /// <summary>
    /// Gets the current velocity of the camera.
    /// </summary>
    public Vector3 Velocity => _velocity;

    /// <summary>
    /// Updates the camera component, handling physics and input.
    /// </summary>
    /// <param name="gameTime">The game time information.</param>
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
        var oldPosition = _position;
        var halfSize = BoundingBoxSize * 0.5f;
        
        _isOnGround = false;

        // Risolvi collisioni per ogni asse separatamente per evitare che si blocchino a vicenda
        var resolved = oldPosition;

        // 1. Risolvi movimento X (est/ovest)
        if (Math.Abs(newPosition.X - oldPosition.X) > 0.001f)
        {
            var testX = new Vector3(newPosition.X, oldPosition.Y, oldPosition.Z);
            var isColliding = IsPositionColliding(testX, halfSize);
            
            // Debug solo per X-axis quando c'è movimento significativo
            if (Math.Abs(newPosition.X - oldPosition.X) > 0.1f)
            {
                Console.WriteLine($"[X-AXIS] Moving from {oldPosition.X:F2} to {newPosition.X:F2}, collision={isColliding}");
            }
            
            if (!isColliding)
            {
                resolved.X = newPosition.X;
            }
            else
            {
                Console.WriteLine($"[X-AXIS BLOCKED] Wall hit! Stopped at X={resolved.X:F2}");
            }
        }

        // 2. Risolvi movimento Z (nord/sud)
        if (Math.Abs(newPosition.Z - oldPosition.Z) > 0.001f)
        {
            var testZ = new Vector3(resolved.X, oldPosition.Y, newPosition.Z);
            if (!IsPositionColliding(testZ, halfSize))
            {
                resolved.Z = newPosition.Z;
            }
        }

        // 3. Risolvi movimento Y (su/giù) - più complesso per gestire gravità e salti
        if (Math.Abs(newPosition.Y - oldPosition.Y) > 0.001f)
        {
            var testY = new Vector3(resolved.X, newPosition.Y, resolved.Z);
            
            if (_velocity.Y <= 0) // Cadendo o fermo
            {
                // Testa la nuova posizione Y
                if (IsPositionColliding(testY, halfSize))
                {
                    // Collisione rilevata, trova il terreno corretto
                    var groundLevel = FindGroundLevel(resolved.X, resolved.Z, oldPosition.Y, halfSize);
                    if (groundLevel.HasValue)
                    {
                        resolved.Y = groundLevel.Value + halfSize.Y;
                        _velocity.Y = 0;
                        _isOnGround = true;
                    }
                    else
                    {
                        // Fallback: usa la posizione precedente
                        resolved.Y = oldPosition.Y;
                        _velocity.Y = 0;
                    }
                }
                else
                {
                    // Nessuna collisione, usa la nuova posizione
                    resolved.Y = newPosition.Y;
                    
                    // Controlla se siamo ancora sul terreno (piccolo controllo sotto i piedi)
                    var feetCheck = new Vector3(resolved.X, resolved.Y - halfSize.Y - 0.05f, resolved.Z);
                    _isOnGround = CheckCollision(feetCheck, Vector3.Zero);
                }
            }
            else // Saltando
            {
                // Controlla collisione con il soffitto
                if (IsPositionColliding(testY, halfSize))
                {
                    _velocity.Y = 0; // Ferma il salto
                }
                else
                {
                    resolved.Y = newPosition.Y;
                }
            }
        }

        return resolved;
    }

    private bool IsPositionColliding(Vector3 position, Vector3 halfSize)
    {
        if (CheckCollision == null) return false;

        // Controlla gli 8 angoli del bounding box del giocatore
        var corners = new Vector3[]
        {
            new(position.X - halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z), // Bottom-back-left
            new(position.X + halfSize.X, position.Y - halfSize.Y, position.Z - halfSize.Z), // Bottom-back-right
            new(position.X - halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z), // Bottom-front-left
            new(position.X + halfSize.X, position.Y - halfSize.Y, position.Z + halfSize.Z), // Bottom-front-right
            new(position.X - halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z), // Top-back-left
            new(position.X + halfSize.X, position.Y + halfSize.Y, position.Z - halfSize.Z), // Top-back-right
            new(position.X - halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z), // Top-front-left
            new(position.X + halfSize.X, position.Y + halfSize.Y, position.Z + halfSize.Z)  // Top-front-right
        };

        // Se qualsiasi angolo è dentro un blocco solido, c'è collisione
        for (int i = 0; i < corners.Length; i++)
        {
            var corner = corners[i];
            var isBlocked = CheckCollision(corner, Vector3.Zero);
            
            if (EnableCollisionDebug && isBlocked)
            {
                Console.WriteLine($"Corner {i} collision at ({corner.X:F2},{corner.Y:F2},{corner.Z:F2})");
            }
            
            if (isBlocked)
            {
                return true;
            }
        }

        return false;
    }

    private float? FindGroundLevel(float x, float z, float startY, Vector3 halfSize)
    {
        if (CheckCollision == null) return null;

        // Cerca il blocco solido più alto sotto la posizione corrente
        for (int blockY = (int)MathF.Floor(startY); blockY >= (int)MathF.Floor(startY) - 10; blockY--)
        {
            // Testa il centro del blocco per vedere se è solido
            var testPos = new Vector3(x, blockY + 0.5f, z);
            if (CheckCollision(testPos, Vector3.Zero))
            {
                // Blocco solido trovato, ritorna la superficie superiore
                return blockY + 1.0f;
            }
        }

        return null; // Nessun terreno trovato
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
                
                // Testa il movimento con collisioni
                var newPosition = _position + horizontalMovement;
                var halfSize = BoundingBoxSize * 0.5f;
                
                // Testa X separatamente
                var testX = new Vector3(newPosition.X, _position.Y, _position.Z);
                if (!IsPositionColliding(testX, halfSize))
                {
                    _position.X = newPosition.X;
                }
                
                // Testa Z separatamente  
                var testZ = new Vector3(_position.X, _position.Y, newPosition.Z);
                if (!IsPositionColliding(testZ, halfSize))
                {
                    _position.Z = newPosition.Z;
                }
                
                _viewDirty = true;
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
        if (!IsMouseCaptured)
        {
            _firstMouseMove = true;
            return;
        }

        var viewport = _graphicsDevice.Viewport;
        var centerX = viewport.Width / 2;
        var centerY = viewport.Height / 2;

        var mouseState = Mouse.GetState();
        var currentMousePosition = new Point(mouseState.X, mouseState.Y);

        if (_firstMouseMove)
        {
            _lastMousePosition = new Point(centerX, centerY);
            _firstMouseMove = false;
            Mouse.SetPosition(centerX, centerY);
            return;
        }

        var deltaX = currentMousePosition.X - centerX;
        var deltaY = currentMousePosition.Y - centerY;

        if (deltaX != 0 || deltaY != 0)
        {
            var xOffset = deltaX * MouseSensitivity;
            var yOffset = deltaY * MouseSensitivity;
            
            ModifyDirection(xOffset, yOffset);
            
            Mouse.SetPosition(centerX, centerY);
        }
    }
}
