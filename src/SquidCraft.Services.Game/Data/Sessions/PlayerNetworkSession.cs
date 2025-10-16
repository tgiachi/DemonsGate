using System.Numerics;
using SquidCraft.Game.Data.Types;
using SquidCraft.Services.Game.Interfaces;

namespace SquidCraft.Services.Game.Data.Sessions;

public class PlayerNetworkSession : IDisposable
{
    public delegate void PositionChangedHandler(PlayerNetworkSession session, Vector3 position);
    public delegate void FacingChangedHandler(PlayerNetworkSession session, Vector3 forward);

    public event PositionChangedHandler OnPositionChanged;
    public event FacingChangedHandler OnFacingChanged;

    public SideType SideView { get; set; }

    /// <summary>
    /// Set of chunk positions that have been sent to this player.
    /// </summary>
    private readonly HashSet<Vector3> _sentChunks = new();

    public INetworkManagerService NetworkManagerService { get; set; }

    public int SessionId { get; set; }

    public bool IsLoggedIn { get; set; }

    public DateTime LastPing { get; set; }

    private Vector3 _position;

    private Vector3 _rotation;

    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            OnPositionChanged?.Invoke(this, _position = value);
        }
    }

    public Vector3 Rotation
    {
        set
        {
            var normalizedFacing = value;
            if (normalizedFacing.LengthSquared() > 0f)
            {
                normalizedFacing = Vector3.Normalize(normalizedFacing);
            }

            if (_rotation == normalizedFacing) return;
            OnFacingChanged?.Invoke(this, _rotation = normalizedFacing);

            // Update SideView based on camera direction
            SideView = CalculateSideViewFromDirection(_rotation);
        }
        get => _rotation;
    }

    /// <summary>
    /// Calculates which side the player is facing based on the camera direction vector.
    /// </summary>
    /// <param name="direction">The normalized direction vector of the camera.</param>
    /// <returns>The SideType representing which direction the player is facing.</returns>
    private static SideType CalculateSideViewFromDirection(Vector3 direction)
    {
        // Find which axis has the largest absolute value
        float absX = Math.Abs(direction.X);
        float absY = Math.Abs(direction.Y);
        float absZ = Math.Abs(direction.Z);

        // Determine the dominant axis and direction
        if (absX > absY && absX > absZ)
        {
            return direction.X > 0 ? SideType.East : SideType.West;
        }

        if (absY > absX && absY > absZ)
        {
            return direction.Y > 0 ? SideType.Top : SideType.Bottom;
        }

        return direction.Z > 0 ? SideType.South : SideType.North;
    }

    /// <summary>
    /// Checks if a chunk at the specified position has been sent to this player.
    /// </summary>
    /// <param name="chunkPosition">The chunk position to check.</param>
    /// <returns>True if the chunk has been sent; otherwise, false.</returns>
    public bool HasChunkBeenSent(Vector3 chunkPosition)
    {
        return _sentChunks.Contains(chunkPosition);
    }

    /// <summary>
    /// Marks a chunk as sent to this player.
    /// </summary>
    /// <param name="chunkPosition">The chunk position to mark as sent.</param>
    public void MarkChunkAsSent(Vector3 chunkPosition)
    {
        _sentChunks.Add(chunkPosition);
    }

    /// <summary>
    /// Marks multiple chunks as sent to this player.
    /// </summary>
    /// <param name="chunkPositions">The chunk positions to mark as sent.</param>
    public void MarkChunksAsSent(IEnumerable<Vector3> chunkPositions)
    {
        foreach (var position in chunkPositions)
        {
            _sentChunks.Add(position);
        }
    }

    /// <summary>
    /// Filters a collection of chunk positions to return only those that haven't been sent yet.
    /// </summary>
    /// <param name="chunkPositions">The chunk positions to filter.</param>
    /// <returns>A collection of chunk positions that haven't been sent to this player.</returns>
    public IEnumerable<Vector3> FilterUnsentChunks(IEnumerable<Vector3> chunkPositions)
    {
        return chunkPositions.Where(pos => !_sentChunks.Contains(pos));
    }

    /// <summary>
    /// Clears all tracked sent chunks.
    /// </summary>
    public void ClearSentChunks()
    {
        _sentChunks.Clear();
    }

    /// <summary>
    /// Gets the count of chunks that have been sent to this player.
    /// </summary>
    public int SentChunkCount => _sentChunks.Count;

    public void Dispose()
    {
        OnPositionChanged = null;
        OnFacingChanged = null;
        Position = default;
        Rotation = default;
        LastPing = default;
        _sentChunks.Clear();
        GC.SuppressFinalize(this);
    }
}
