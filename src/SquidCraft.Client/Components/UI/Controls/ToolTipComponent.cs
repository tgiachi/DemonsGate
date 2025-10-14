using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Lightweight tooltip panel that displays contextual text.
/// </summary>
public class ToolTipComponent : BaseComponent
{
    private readonly ScrollingTextBoxComponent _textBox;

    public ToolTipComponent()
    {
        BackgroundColor = new Color(45, 45, 48, 230);
        BorderColor = new Color(75, 80, 85, 255);
        Padding = new Vector2(8f, 6f);

        _textBox = new ScrollingTextBoxComponent(fontSize: 14)
        {
            AutoScroll = false,
            Padding = Vector2.Zero,
            BackgroundColor = Color.Transparent,
            BorderColor = Color.Transparent,
            Size = new Vector2(200, 80)
        };

        AddChild(_textBox);
        TextColor = Color.White;
        IsVisible = false;
    }

    public Color BackgroundColor { get; set; }

    public Color BorderColor { get; set; }

    public Color TextColor
    {
        get => _textBox.TextColor;
        set => _textBox.TextColor = value;
    }

    public Vector2 Padding { get; set; }

    public string Text
    {
        get => string.Join("\n", _textBox.Lines);
        set
        {
            _textBox.Clear();
            var content = value ?? string.Empty;
            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                _textBox.AppendLine(line);
            }
            _textBox.ScrollToEnd();
            UpdateLayout();
        }
    }

    public void Show(Vector2 position, string text)
    {
        Position = position;
        Text = text;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        UpdateLayout();
    }

    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (!IsVisible)
        {
            return;
        }

        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();
        var absolute = Position + parentPosition;
        var resolvedSize = ResolveSize();

        var bounds = new Rectangle(
            (int)absolute.X,
            (int)absolute.Y,
            (int)resolvedSize.X,
            (int)resolvedSize.Y);

        spriteBatch.Draw(pixel, bounds, BackgroundColor * Opacity);
        DrawBorder(spriteBatch, pixel, bounds, BorderColor * Opacity);
    }

    private void UpdateLayout()
    {
        var textSize = MeasureText();
        var newSize = textSize + Padding * 2f;
        base.Size = new Vector2(Math.Max(newSize.X, 0f), Math.Max(newSize.Y, 0f));
        _textBox.Position = Padding;
        _textBox.Size = new Vector2(Math.Max(0f, Size.X - Padding.X * 2f), Math.Max(0f, Size.Y - Padding.Y * 2f));
    }

    private Vector2 MeasureText()
    {
        var font = _textBox.Font;
        if (font == null || _textBox.Lines.Count == 0)
        {
            return new Vector2(0f, 0f);
        }

        var totalHeight = 0f;
        var maxWidth = 0f;

        foreach (var line in _textBox.Lines)
        {
            var size = font.MeasureString(line);
            maxWidth = Math.Max(maxWidth, size.X);
            totalHeight += size.Y;
        }

        return new Vector2(maxWidth, totalHeight);
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }
}
