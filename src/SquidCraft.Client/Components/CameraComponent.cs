using Microsoft.Xna.Framework;
using System.Collections.Generic;
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

    private float _verticalVelocity;
    private const float Gravity = 32f;
    private const float JumpVelocity = 12f;
    private const float TerminalVelocity = 50f;

    // Spatial hashing for collision detection optimization
    private const float CellSize = 1.0f;
    private readonly Dictionary<(int, int, int), HashSet<Vector3>> _spatialGrid = new();
    private bool _isOnGround;

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
    /// Gets or sets a value indicating whether fly mode is enabled (disables physics for creative flight).
    /// </summary>
    public bool FlyMode { get; set; } = false;

    public Func<Vector3, bool>? IsBlockSolid { get; set; }

    public Vector3 BoundingBoxSize { get; set; } = new Vector3(0.6f, 1.8f, 0.6f);

    public bool IsOnGround => _isOnGround;
    
    /// <summary>
    /// Updates the camera component, handling physics and input.
    /// </summary>
    /// <param name="gameTime">The game time information.</param>
    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (EnableInput)
        {
            HandleKeyboardInput(deltaTime);
            HandleMouseInput();
        }
    }

    private void HandleKeyboardInput(float deltaTime)
    {
        var keyboardState = Keyboard.GetState();

        if (!FlyMode)
        {
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

            var movement = Vector3.Zero;
            if (keyboardState.IsKeyDown(Keys.W))
            {
                movement += forwardFlat;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                movement -= forwardFlat;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                movement -= rightFlat;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                movement += rightFlat;
            }

            if (movement != Vector3.Zero)
            {
                movement.Normalize();
                movement *= MoveSpeed * deltaTime;
                
                var newPos = _position + new Vector3(movement.X, 0, 0);
                if (!CheckCollision(newPos))
                {
                    _position.X = newPos.X;
                    _viewDirty = true;
                }

                newPos = _position + new Vector3(0, 0, movement.Z);
                if (!CheckCollision(newPos))
                {
                    _position.Z = newPos.Z;
                    _viewDirty = true;
                }
            }

            if (keyboardState.IsKeyDown(Keys.Space) && _isOnGround)
            {
                _verticalVelocity = JumpVelocity;
                _isOnGround = false;
            }

            ApplyPhysics(deltaTime);
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

    private void ApplyPhysics(float deltaTime)
    {
        _verticalVelocity -= Gravity * deltaTime;
        _verticalVelocity = MathHelper.Clamp(_verticalVelocity, -TerminalVelocity, TerminalVelocity);

        var verticalMovement = _verticalVelocity * deltaTime;
        var newPos = _position + new Vector3(0, verticalMovement, 0);

        if (verticalMovement < 0)
        {
            if (CheckGroundCollision(newPos))
            {
                _position.Y = MathF.Floor(_position.Y - BoundingBoxSize.Y / 2) + BoundingBoxSize.Y / 2 + 0.01f;
                _verticalVelocity = 0;
                _isOnGround = true;
                _viewDirty = true;
            }
            else
            {
                _position.Y = newPos.Y;
                _isOnGround = false;
                _viewDirty = true;
            }
        }
        else
        {
            if (!CheckCollision(newPos))
            {
                _position.Y = newPos.Y;
                _isOnGround = false;
                _viewDirty = true;
            }
            else
            {
                _verticalVelocity = 0;
            }
        }
    }

    private bool CheckCollision(Vector3 position)
    {
        // Use optimized spatial grid collision detection if available
        if (_spatialGrid.Count > 0)
        {
            return CheckCollisionOptimized(position);
        }

        // Fallback to original method if spatial grid is not populated
        if (IsBlockSolid == null) return false;

        var halfSize = BoundingBoxSize / 2;
        var min = position - halfSize;
        var max = position + halfSize;

        for (float x = min.X; x <= max.X; x += 0.5f)
        {
            for (float y = min.Y; y <= max.Y; y += 0.5f)
            {
                for (float z = min.Z; z <= max.Z; z += 0.5f)
                {
                    if (IsBlockSolid(new Vector3(x, y, z)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool CheckGroundCollision(Vector3 position)
    {
        // Use optimized spatial grid collision detection if available
        if (_spatialGrid.Count > 0)
        {
            return CheckGroundCollisionOptimized(position);
        }

        // Fallback to original method if spatial grid is not populated
        if (IsBlockSolid == null) return false;

        var feetY = position.Y - BoundingBoxSize.Y / 2 - 0.01f;
        var halfWidth = BoundingBoxSize.X / 2;
        var halfDepth = BoundingBoxSize.Z / 2;

        var testPositions = new[]
        {
            new Vector3(position.X - halfWidth, feetY, position.Z - halfDepth),
            new Vector3(position.X + halfWidth, feetY, position.Z - halfDepth),
            new Vector3(position.X - halfWidth, feetY, position.Z + halfDepth),
            new Vector3(position.X + halfWidth, feetY, position.Z + halfDepth),
            new Vector3(position.X, feetY, position.Z)
        };

        foreach (var testPos in testPositions)
        {
            if (IsBlockSolid(testPos))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Populates the spatial grid with solid blocks for efficient collision detection.
    /// Call this when the world changes or IsBlockSolid delegate is updated.
    /// </summary>
    /// <param name="worldBounds">The bounds of the world to scan for solid blocks.</param>
    public void PopulateSpatialGrid(BoundingBox worldBounds)
    {
        _spatialGrid.Clear();

        var minCellX = (int)MathF.Floor(worldBounds.Min.X / CellSize);
        var maxCellX = (int)MathF.Ceiling(worldBounds.Max.X / CellSize);
        var minCellY = (int)MathF.Floor(worldBounds.Min.Y / CellSize);
        var maxCellY = (int)MathF.Ceiling(worldBounds.Max.Y / CellSize);
        var minCellZ = (int)MathF.Floor(worldBounds.Min.Z / CellSize);
        var maxCellZ = (int)MathF.Ceiling(worldBounds.Max.Z / CellSize);

        for (var cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (var cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                for (var cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    var cellKey = (cellX, cellY, cellZ);
                    var cellBlocks = new HashSet<Vector3>();

                    // Scan all blocks in this cell
                    var cellMin = new Vector3(cellX * CellSize, cellY * CellSize, cellZ * CellSize);
                    var cellMax = cellMin + new Vector3(CellSize, CellSize, CellSize);

                    for (float x = cellMin.X; x < cellMax.X; x += 0.5f)
                    {
                        for (float y = cellMin.Y; y < cellMax.Y; y += 0.5f)
                        {
                            for (float z = cellMin.Z; z < cellMax.Z; z += 0.5f)
                            {
                                var blockPos = new Vector3(x, y, z);
                                if (IsBlockSolid?.Invoke(blockPos) == true)
                                {
                                    cellBlocks.Add(blockPos);
                                }
                            }
                        }
                    }

                    if (cellBlocks.Count > 0)
                    {
                        _spatialGrid[cellKey] = cellBlocks;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Optimized collision detection using spatial grid.
    /// </summary>
    private bool CheckCollisionOptimized(Vector3 position)
    {
        if (IsBlockSolid == null) return false;

        var halfSize = BoundingBoxSize / 2;
        var min = position - halfSize;
        var max = position + halfSize;

        // Get cells that intersect with the bounding box
        var minCellX = (int)MathF.Floor(min.X / CellSize);
        var maxCellX = (int)MathF.Ceiling(max.X / CellSize);
        var minCellY = (int)MathF.Floor(min.Y / CellSize);
        var maxCellY = (int)MathF.Ceiling(max.Y / CellSize);
        var minCellZ = (int)MathF.Floor(min.Z / CellSize);
        var maxCellZ = (int)MathF.Ceiling(max.Z / CellSize);

        // Check all intersecting cells
        for (var cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (var cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                for (var cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
                {
                    var cellKey = (cellX, cellY, cellZ);
                    if (_spatialGrid.TryGetValue(cellKey, out var cellBlocks))
                    {
                        // Check if any block in this cell intersects with our bounding box
                        foreach (var blockPos in cellBlocks)
                        {
                            if (blockPos.X >= min.X && blockPos.X <= max.X &&
                                blockPos.Y >= min.Y && blockPos.Y <= max.Y &&
                                blockPos.Z >= min.Z && blockPos.Z <= max.Z)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Optimized ground collision detection using spatial grid.
    /// </summary>
    private bool CheckGroundCollisionOptimized(Vector3 position)
    {
        if (IsBlockSolid == null) return false;

        var feetY = position.Y - BoundingBoxSize.Y / 2 - 0.01f;
        var halfWidth = BoundingBoxSize.X / 2;
        var halfDepth = BoundingBoxSize.Z / 2;

        var testPositions = new[]
        {
            new Vector3(position.X - halfWidth, feetY, position.Z - halfDepth),
            new Vector3(position.X + halfWidth, feetY, position.Z - halfDepth),
            new Vector3(position.X - halfWidth, feetY, position.Z + halfDepth),
            new Vector3(position.X + halfWidth, feetY, position.Z + halfDepth),
            new Vector3(position.X, feetY, position.Z)
        };

        // Get the cell for the ground level
        var cellY = (int)MathF.Floor(feetY / CellSize);
        var minCellX = (int)MathF.Floor((position.X - halfWidth) / CellSize);
        var maxCellX = (int)MathF.Ceiling((position.X + halfWidth) / CellSize);
        var minCellZ = (int)MathF.Floor((position.Z - halfDepth) / CellSize);
        var maxCellZ = (int)MathF.Ceiling((position.Z + halfDepth) / CellSize);

        // Check intersecting cells at ground level
        for (var cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (var cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
            {
                var cellKey = (cellX, cellY, cellZ);
                if (_spatialGrid.TryGetValue(cellKey, out var cellBlocks))
                {
                    foreach (var blockPos in cellBlocks)
                    {
                        foreach (var testPos in testPositions)
                        {
                            if (Vector3.DistanceSquared(blockPos, testPos) < 0.01f) // Small epsilon for floating point comparison
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }
}
