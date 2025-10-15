using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     GroupBox component for grouping other UI components with a border and title
/// </summary>
public class GroupBoxComponent : BaseComponent
{
    private IAssetManagerService _assetManagerService;
    private SpriteFontBase? _font;

    /// <summary>
    ///     Initializes a new GroupBox component
    /// </summary>
    /// <param name="text">The title text to display</param>
    /// <param name="width">Width of the group box</param>
    /// <param name="height">Height of the group box</param>
    public GroupBoxComponent(string text = "", float width = 200f, float height = 100f)
    {
        Text = text;
        Size = new Vector2(width, height);

        // Default styling
        SetDefaultColors();
    }

    /// <summary>
    ///     Gets or sets the title text
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    ///     Gets or sets whether the group box is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Padding inside the group box
    /// </summary>
    public float Padding { get; set; } = 8f;

    /// <summary>
    ///     Border width
    /// </summary>
    public int BorderWidth { get; set; } = 1;

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color TextColor { get; set; }
    public Color DisabledTextColor { get; set; }

    /// <summary>
    ///     Position of the component
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    ///     Size of the component
    /// </summary>
    public Vector2 Size { get; set; }

    private GraphicsDevice GraphicsDevice { get; set; }

    /// <summary>
    ///     Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        BackgroundColor = new Color(248, 248, 248);
        BorderColor = new Color(118, 118, 118);
        TextColor = Color.Black;
        DisabledTextColor = Color.Gray;
    }

    /// <summary>
    ///     Initializes the component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _assetManagerService = SquidCraftClientContext.AssetManagerService;
        GraphicsDevice = SquidCraftClientContext.GraphicsDevice;

        LoadFont();
    }

    /// <summary>
    ///     Loads the font
    /// </summary>
    private void LoadFont()
    {
        _font = _assetManagerService.GetFontTtf("DefaultFont", 12);
    }

    /// <summary>
    ///     Gets the content area bounds (inside padding)
    /// </summary>
    public Rectangle GetContentBounds()
    {
        return new Rectangle(
            (int)(Position.X + Padding),
            (int)(Position.Y + Padding + (_font?.LineHeight ?? 0) / 2),
            (int)(Size.X - 2 * Padding),
            (int)(Size.Y - 2 * Padding - (_font?.LineHeight ?? 0) / 2)
        );
    }

    /// <summary>
    ///     Draws the component content
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="parentPosition">Parent position offset</param>
    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (_assetManagerService.GetPixelTexture() == null || _font == null)
        {
            return;
        }

        var position = Position + parentPosition;
        var bounds = new Rectangle(
            (int)position.X,
            (int)position.Y,
            (int)Size.X,
            (int)Size.Y
        );

        // Draw background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), bounds, BackgroundColor);

        // Draw border
        DrawBorder(spriteBatch, bounds);

        // Draw title text
        if (!string.IsNullOrEmpty(Text))
        {
            var textColor = IsEnabled ? TextColor : DisabledTextColor;
            var textSize = _font.MeasureString(Text);
            var textPosition = new Vector2(
                position.X + Padding,
                position.Y
            );

            // Draw background for text to cover the border
            var textBgRect = new Rectangle(
                (int)textPosition.X - 2,
                (int)textPosition.Y,
                (int)textSize.X + 4,
                (int)_font.LineHeight
            );
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), textBgRect, BackgroundColor);

            spriteBatch.DrawString(_font, Text, textPosition, textColor);
        }
    }

    /// <summary>
    ///     Draws the group box border
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var pixel = _assetManagerService.GetPixelTexture();
        if (pixel == null)
        {
            return;
        }

        var textWidth = 0f;
        if (!string.IsNullOrEmpty(Text) && _font != null)
        {
            textWidth = _font.MeasureString(Text).X + 8; // Extra padding
        }

        // Top border (left part)
        if (textWidth > 0)
        {
            spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, (int)(textWidth / 2 - 2), BorderWidth), BorderColor);
            // Top border (right part)
            spriteBatch.Draw(pixel, new Rectangle(bounds.X + (int)(textWidth / 2 + textWidth / 2 + 4), bounds.Y, bounds.Width - (int)(textWidth / 2 + textWidth / 2 + 4), BorderWidth), BorderColor);
        }
        else
        {
            spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, BorderWidth), BorderColor);
        }

        // Bottom border
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - BorderWidth, bounds.Width, BorderWidth), BorderColor);

        // Left border
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, BorderWidth, bounds.Height), BorderColor);

        // Right border
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - BorderWidth, bounds.Y, BorderWidth, bounds.Height), BorderColor);
    }
}