using System;
using System.Collections.Generic;
using System.Text;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Multiline text box with vertical scroll support. Designed for console/log-style output.
/// </summary>
public class ScrollingTextBoxComponent : BaseComponent
{
    private readonly List<string> _lines = new();
    private SpriteFontBase? _font;
    private int _fontSize;
    private int _scrollOffset;
    private bool _autoScroll = true;

    public ScrollingTextBoxComponent(
        IEnumerable<string>? lines = null,
        string fontName = "DefaultFont",
        int fontSize = 14,
        Vector2? position = null,
        Vector2? size = null)
    {
        FontName = fontName;
        FontSize = Math.Max(8, fontSize);
        Position = position ?? Vector2.Zero;
        base.Size = size ?? new Vector2(360f, 200f);

        BackgroundColor = new Color(18, 20, 22);
        BorderColor = new Color(56, 62, 68);
        TextColor = new Color(221, 221, 221);
        SelectionColor = new Color(73, 80, 87, 150);
        Padding = new Vector2(12f, 10f);
        LineSpacing = 2f;

        LoadFont();

        if (lines != null)
        {
            foreach (var line in lines)
            {
                AppendLine(line);
            }
            ScrollToEnd();
        }
    }

    /// <summary>
    /// Gets or sets the font asset name.
    /// </summary>
    public string FontName { get; }

    /// <summary>
    /// Gets or sets the font size used for rendering.
    /// </summary>
    public int FontSize
    {
        get => _fontSize;
        set
        {
            var clamped = Math.Max(8, value);
            if (_fontSize != clamped)
            {
                _fontSize = clamped;
                LoadFont();
            }
        }
    }

    /// <summary>
    /// Gets the lines currently stored in the scrolling text box.
    /// </summary>
    public IReadOnlyList<string> Lines => _lines;

    /// <summary>
    /// Gets the underlying font used for rendering.
    /// </summary>
    public SpriteFontBase? Font => _font;

    /// <summary>
    /// Gets or sets whether the view auto-scrolls when new lines are appended.
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set => _autoScroll = value;
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    public Color TextColor { get; set; }

    /// <summary>
    /// Gets or sets the selection color used for highlighting lines.
    /// </summary>
    public Color SelectionColor { get; set; }

    /// <summary>
    /// Gets or sets the padding applied inside the text box.
    /// </summary>
    public Vector2 Padding { get; set; }

    /// <summary>
    /// Gets or sets the vertical spacing between lines.
    /// </summary>
    public float LineSpacing { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of lines stored. 0 disables trimming.
    /// </summary>
    public int MaxLines { get; set; } = 500;

    /// <summary>
    /// Appends a new line to the text box.
    /// </summary>
    public void AppendLine(string line)
    {
        _lines.Add(line);

        if (MaxLines > 0 && _lines.Count > MaxLines)
        {
            var overflow = _lines.Count - MaxLines;
            _lines.RemoveRange(0, overflow);
            _scrollOffset = Math.Max(0, _scrollOffset - overflow);
        }

        if (_autoScroll)
        {
            ScrollToEnd();
        }
    }

    /// <summary>
    /// Appends multiple lines at once.
    /// </summary>
    public void AppendLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            AppendLine(line);
        }
    }

    /// <summary>
    /// Clears all lines.
    /// </summary>
    public void Clear()
    {
        _lines.Clear();
        _scrollOffset = 0;
    }

    /// <summary>
    /// Scrolls the view to the last line.
    /// </summary>
    public void ScrollToEnd()
    {
        var visibleLines = GetVisibleLineCount();
        _scrollOffset = Math.Max(0, _lines.Count - visibleLines);
    }

    public override void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        var wheelValue = mouseState.ScrollWheelValue;
        if (_previousWheel != wheelValue)
        {
            var delta = wheelValue - _previousWheel;
            _previousWheel = wheelValue;

            var visibleLines = GetVisibleLineCount();
            var direction = delta > 0 ? -1 : 1;
            _scrollOffset = Math.Clamp(_scrollOffset + direction, 0, Math.Max(0, _lines.Count - visibleLines));
            _autoScroll = _scrollOffset >= Math.Max(0, _lines.Count - visibleLines);
        }

        base.HandleMouse(mouseState, gameTime);
    }
    private int _previousWheel;

    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (_font == null)
        {
            return;
        }

        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();
        var absolute = Position + parentPosition;
        var size = ResolveSize();

        var bounds = new Rectangle(
            (int)absolute.X,
            (int)absolute.Y,
            (int)size.X,
            (int)size.Y);

        spriteBatch.Draw(pixel, bounds, BackgroundColor * Opacity);
        DrawBorder(spriteBatch, pixel, bounds);

        var textArea = new Rectangle(
            (int)(absolute.X + Padding.X),
            (int)(absolute.Y + Padding.Y),
            Math.Max(0, (int)(size.X - Padding.X * 2f)),
            Math.Max(0, (int)(size.Y - Padding.Y * 2f)));

        if (textArea.Width <= 0 || textArea.Height <= 0)
        {
            return;
        }

        var graphicsDevice = spriteBatch.GraphicsDevice;
        var previousScissor = graphicsDevice.ScissorRectangle;
        var previousRasterizer = graphicsDevice.RasterizerState;

        var newScissor = Rectangle.Intersect(previousScissor, textArea);
        if (newScissor.Width <= 0 || newScissor.Height <= 0)
        {
            return;
        }

        graphicsDevice.RasterizerState = RasterizerState.CullNone;
        graphicsDevice.ScissorRectangle = newScissor;

        var lineHeight = _font.LineHeight + LineSpacing;
        var startIndex = Math.Clamp(_scrollOffset, 0, Math.Max(0, _lines.Count - 1));
        var maxLines = GetVisibleLineCount();

        for (int i = 0; i < maxLines && startIndex + i < _lines.Count; i++)
        {
            var line = _lines[startIndex + i];
            var linePosition = new Vector2(textArea.X, textArea.Y + i * lineHeight);
            spriteBatch.DrawString(_font, line, linePosition, TextColor * Opacity);
        }

        graphicsDevice.ScissorRectangle = previousScissor;
        graphicsDevice.RasterizerState = previousRasterizer;
    }

    private int GetVisibleLineCount()
    {
        if (_font == null)
        {
            return 0;
        }

        var lineHeight = _font.LineHeight + LineSpacing;
        var areaHeight = ResolveSize().Y - Padding.Y * 2f;
        return lineHeight <= 0 ? 0 : Math.Max(1, (int)Math.Floor(areaHeight / lineHeight));
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), BorderColor * Opacity);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), BorderColor * Opacity);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), BorderColor * Opacity);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), BorderColor * Opacity);
    }

    private void LoadFont()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf(FontName, _fontSize);
    }
}
