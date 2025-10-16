using Microsoft.Xna.Framework;

namespace SquidCraft.Client.Services;

public sealed class DynamicLight
{
    public Vector3 Position { get; set; }
    public Color Color { get; set; }
    public float Radius { get; set; }
    public float Intensity { get; set; } = 1f;
}

public sealed class DynamicLightingService
{
    private readonly List<DynamicLight> _lights = new();

    public IReadOnlyList<DynamicLight> Lights => _lights;

    public DynamicLight AddLight(Vector3 position, Color color, float radius, float intensity = 1f)
    {
        var light = new DynamicLight
        {
            Position = position,
            Color = color,
            Radius = radius,
            Intensity = intensity
        };
        _lights.Add(light);
        return light;
    }

    public void RemoveLight(DynamicLight light)
    {
        _lights.Remove(light);
    }

    public void Clear()
    {
        _lights.Clear();
    }

    public Color CalculateLightingAt(Vector3 position, Color baseColor)
    {
        var totalLight = Vector3.Zero;

        foreach (var light in _lights)
        {
            var distance = Vector3.Distance(position, light.Position);
            if (distance < light.Radius)
            {
                var attenuation = 1f - (distance / light.Radius);
                attenuation = MathF.Pow(attenuation, 1.5f);
                
                var lightContribution = new Vector3(
                    light.Color.R / 255f,
                    light.Color.G / 255f,
                    light.Color.B / 255f
                ) * light.Intensity * attenuation;

                totalLight += lightContribution;
            }
        }

        if (totalLight.LengthSquared() > 0.01f)
        {
            var r = MathHelper.Clamp(baseColor.R / 255f + totalLight.X, 0f, 1.5f);
            var g = MathHelper.Clamp(baseColor.G / 255f + totalLight.Y, 0f, 1.5f);
            var b = MathHelper.Clamp(baseColor.B / 255f + totalLight.Z, 0f, 1.5f);

            return new Color(r, g, b, baseColor.A / 255f);
        }

        return baseColor;
    }
}
