using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Simple rectangular progress bar with optional textual indicator.
/// </summary>
public class ProgressBarComponent : BaseComponent
{
    private float _minimum = 0f;
    private float _maximum = 100f;
    private float _value;
    private SpriteFontBase? _font;

    /// <summary>
    /// Initializes a new <see cref="ProgressBarComponent"/>.
    /// </summary>
    public ProgressBarComponent(
        Vector2? position = null,
        Vector2? size = null,
        string fontName = "DefaultFont",
        int fontSize = 14)
    {
        Position = position ?? Vector2.Zero;
        base.Size = size ?? new Vector2(180f, 20f);

        FontName = fontName;
        FontSize = Math.Max(8, fontSize);

        BackgroundColor = new Color(52, 58, 64);
        FillColor = new Color(76, 175, 80);
        BorderColor = new Color(33, 37, 41);
        TextColor = Color.White;
        LabelFormat = "{0:P0}";
        ShowLabel = true;
        Padding = new Vector2(2f, 2f);

        LoadFont();
    }

    /// <summary>
    /// Fired when the value changes.
    /// </summary>
    public event EventHandler<float>? ValueChanged;

    /// <summary>
    /// Gets or sets the font used to render the optional label.
    /// </summary>
    public string FontName { get; }

    /// <summary>
    /// Gets or sets the font size used to render the optional label.
    /// </summary>
    public int FontSize { get; }

    /// <summary>
    /// Gets or sets the bar minimum value.
    /// </summary>
    public float Minimum
    {
        get => _minimum;
        set
        {
            if (_minimum.Equals(value))
            {
                return;
            }

            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            Value = Value; // Re-clamp
        }
    }

    /// <summary>
    /// Gets or sets the bar maximum value.
    /// </summary>
    public float Maximum
    {
        get => _maximum;
        set
        {
            if (_maximum.Equals(value))
            {
                return;
            }

            _maximum = value;
            if (_maximum < _minimum)
            {
                _minimum = _maximum;
            }

            Value = Value; // Re-clamp
        }
    }

    /// <summary>
    /// Gets or sets the current bar value.
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var clamped = MathHelper.Clamp(value, Minimum, Maximum);
            if (_value.Equals(clamped))
            {
                return;
            }

            _value = clamped;
            ValueChanged?.Invoke(this, _value);
        }
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the fill color used for the progress portion.
    /// </summary>
    public Color FillColor { get; set; }

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the text color for the label.
    /// </summary>
    public Color TextColor { get; set; }

    /// <summary>
    /// Gets or sets additional padding within the progress bar.
    /// </summary>
    public Vector2 Padding { get; set; }

    /// <summary>
    /// Gets or sets whether the label is rendered.
    /// </summary>
    public bool ShowLabel { get; set; }

    /// <summary>
    /// Gets or sets a custom label provider. When null, <see cref="LabelFormat"/> is used.
    /// </summary>
    public Func<float, string>? LabelFormatter { get; set; }

    /// <summary>
    /// Gets or sets the string format used when <see cref="LabelFormatter"/> is null.
    /// Receives the normalized value (0 to 1).
    /// </summary>
    public string LabelFormat { get; set; }

    /// <summary>
    /// Gets the normalized progress between 0 and 1.
    /// </summary>
    public float Progress => Maximum.Equals(Minimum)
        ? 0f
        : MathHelper.Clamp((Value - Minimum) / (Maximum - Minimum), 0f, 1f);

    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();
        var absolute = Position + parentPosition;
        var resolvedSize = ResolveSize();

        var backgroundRect = new Rectangle(
            (int)absolute.X,
            (int)absolute.Y,
            (int)resolvedSize.X,
            (int)resolvedSize.Y);

        spriteBatch.Draw(pixel, backgroundRect, BackgroundColor * Opacity);

        var innerWidth = resolvedSize.X - Padding.X * 2f;
        var innerHeight = resolvedSize.Y - Padding.Y * 2f;

        if (innerWidth > 0 && innerHeight > 0)
        {
            var fillWidth = Math.Max(0f, innerWidth * Progress);
            var fillRect = new Rectangle(
                (int)(absolute.X + Padding.X),
                (int)(absolute.Y + Padding.Y),
                (int)Math.Ceiling(fillWidth),
                (int)Math.Ceiling(innerHeight));

            if (fillRect.Width > 0 && fillRect.Height > 0)
            {
                spriteBatch.Draw(pixel, fillRect, FillColor * Opacity);
            }
        }

        DrawBorder(spriteBatch, pixel, backgroundRect);

        if (ShowLabel && _font != null)
        {
            var text = LabelFormatter?.Invoke(Progress) ?? string.Format(LabelFormat, Progress);
            var textSize = _font.MeasureString(text);
            var textPosition = absolute + (resolvedSize - textSize) / 2f;
            spriteBatch.DrawString(_font, text, textPosition, TextColor * Opacity);
        }
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
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf(FontName, FontSize);
    }
}
