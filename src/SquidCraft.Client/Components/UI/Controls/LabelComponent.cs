using Microsoft.Xna.Framework;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Lightweight wrapper over <see cref="TextComponent"/> representing a static label.
/// </summary>
public class LabelComponent : TextComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LabelComponent"/>.
    /// </summary>
    public LabelComponent(
        string text = "",
        string fontName = "DefaultFont",
        int fontSize = 14,
        Vector2? position = null,
        Color? color = null)
        : base(text, fontName, fontSize, position, color)
    {
        IsEnabled = false;
    }
}
