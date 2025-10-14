using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components;

/// <summary>
/// A simple component for rendering text in scenes
/// </summary>
public class TextComponent : BaseComponent
{
    private SpriteFontBase? _font;
    private string _text;
    private int _fontSize;

    /// <summary>
    /// Initializes a new instance of the TextComponent class
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <param name="fontName">The name of the font to use</param>
    /// <param name="fontSize">The font size</param>
    /// <param name="position">The relative position of the text</param>
    /// <param name="color">The color of the text</param>
    public TextComponent(
        string text = "Text",
        string fontName = "DefaultFont",
        int fontSize = 14,
        Vector2? position = null,
        Color? color = null)
    {
        _text = text ?? string.Empty;
        _fontSize = fontSize;
        FontName = fontName;
        Position = position ?? Vector2.Zero;
        Color = color ?? Color.White;

        LoadFont();
        UpdateSize();
    }

    /// <summary>
    /// Gets or sets the text to display
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                UpdateSize();
            }
        }
    }

    /// <summary>
    /// Gets the font name
    /// </summary>
    public string FontName { get; }

    /// <summary>
    /// Gets or sets the font size
    /// </summary>
    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                LoadFont();
                UpdateSize();
            }
        }
    }

    /// <summary>
    /// Gets or sets the color of the text
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// Draws the text content
    /// </summary>
    /// <param name="spriteBatch">The sprite batch to use for drawing</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="parentPosition">The position of the parent</param>
    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (_font == null || string.IsNullOrEmpty(_text))
        {
            return;
        }

        var absolutePosition = Position + parentPosition;
        var drawColor = Color * Opacity;
        spriteBatch.DrawString(_font, _text, absolutePosition, drawColor);
    }

    /// <summary>
    /// Centers the text horizontally on the screen
    /// </summary>
    public void CenterHorizontal()
    {
        if (_font == null)
        {
            return;
        }

        var textSize = base.Size;
        var centerX = (SquidCraftClientContext.GraphicsDevice.Viewport.Width - textSize.X) / 2f;
        Position = new Vector2(centerX, Position.Y);
    }

    /// <summary>
    /// Centers the text vertically on the screen
    /// </summary>
    public void CenterVertical()
    {
        if (_font == null)
        {
            return;
        }

        var textSize = base.Size;
        var centerY = (SquidCraftClientContext.GraphicsDevice.Viewport.Height - textSize.Y) / 2f;
        Position = new Vector2(Position.X, centerY);
    }

    /// <summary>
    /// Centers the text on the screen
    /// </summary>
    public void Center()
    {
        CenterHorizontal();
        CenterVertical();
    }

    /// <summary>
    /// Loads the font
    /// </summary>
    private void LoadFont()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf(FontName, FontSize);
    }

    /// <summary>
    /// Updates the component size based on text dimensions
    /// </summary>
    private void UpdateSize()
    {
        if (_font != null)
        {
            base.Size = _font.MeasureString(_text);
        }
    }


}
