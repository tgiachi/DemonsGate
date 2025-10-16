using Microsoft.Xna.Framework;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     Interface for UI components
/// </summary>
public interface IUIComponent
{
    Vector2 Position { get; set; }
    Vector2 Size { get; set; }
    bool IsEnabled { get; set; }
    bool Visible { get; set; }
}