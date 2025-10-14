using Microsoft.Xna.Framework;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Components.Interfaces;

public interface ISCDrawableComponent : ISCUpdate, ISCDrawable, ISCInputReceiver
{
    string Id { get; }

    string Name { get; }

    Vector2 Position { get; set; }

    Vector2 Size { get; set; }

    Vector2 Scale { get; set; }

    ISCDrawableComponent? Parent { get; }

    IEnumerable<ISCDrawableComponent> Children { get; }

    int ZIndex { get; }

    bool IsVisible { get; }

    bool IsEnabled { get; }

    float Opacity { get; set; }

    float Rotation { get; set; }

    bool IsFocused { get; set; }

    /// <summary>
    /// Checks if a point is within the component's bounds
    /// </summary>
    /// <param name="point">Point to check</param>
    /// <returns>True if point is within bounds</returns>
    bool Contains(Vector2 point);
}
