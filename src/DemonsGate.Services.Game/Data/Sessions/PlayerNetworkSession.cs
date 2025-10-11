using System.Numerics;
using DemonsGate.Services.Game.Interfaces;

namespace DemonsGate.Services.Game.Data.Sessions;

public class PlayerNetworkSession : IDisposable
{
    public delegate void PositionChangedHandler(Vector3 position);
    public delegate void FacingChangedHandler(Vector3 forward);

    public event PositionChangedHandler OnPositionChanged;
    public event FacingChangedHandler OnFacingChanged;


    public INetworkManagerService NetworkManagerService { get; set; }

    public int SessionId { get; set; }

    public DateTime LastPing { get; set; }

    private Vector3 _position;

    private Vector3 _facing;

    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            OnPositionChanged?.Invoke(_position = value);
        }
    }

    public Vector3 Facing
    {
        set
        {
            var normalizedFacing = value;
            if (normalizedFacing.LengthSquared() > 0f)
            {
                normalizedFacing = Vector3.Normalize(normalizedFacing);
            }

            if (_facing == normalizedFacing) return;
            OnFacingChanged?.Invoke(_facing = normalizedFacing);
        }
        get => _facing;
    }



    public void Dispose()
    {
        OnPositionChanged = null;
        OnFacingChanged = null;
        Position = default;
        Facing = default;
        LastPing = default;
        GC.SuppressFinalize(this);
    }
}
