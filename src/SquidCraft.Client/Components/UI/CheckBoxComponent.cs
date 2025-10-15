using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     CheckBox component for boolean values
/// </summary>
public class CheckBoxComponent : BaseComponent
{
    private IAssetManagerService _assetManagerService;
    private SpriteFontBase? _font;
    private MouseState _previousMouseState;
    private bool _isChecked;
    private bool _isHovered;

    /// <summary>
    ///     Initializes a new CheckBox component
    /// </summary>
    /// <param name="text">The text to display next to the checkbox</param>
    public CheckBoxComponent(string text = "")
    {
        Text = text;
        Size = new Vector2(100, 20);

        // Default styling
        SetDefaultColors();
    }

    /// <summary>
    ///     Gets or sets the text displayed next to the checkbox
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    ///     Gets or sets whether the checkbox is checked
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                CheckedChanged?.Invoke(this, new CheckedChangedEventArgs(value));
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether the checkbox is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Size of the checkbox square
    /// </summary>
    public int CheckBoxSize { get; set; } = 16;

    /// <summary>
    ///     Spacing between checkbox and text
    /// </summary>
    public float Spacing { get; set; } = 4f;

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color CheckMarkColor { get; set; }
    public Color TextColor { get; set; }
    public Color DisabledTextColor { get; set; }
    public Color HoverBackgroundColor { get; set; }

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
    ///     Event fired when the checked state changes
    /// </summary>
    public event EventHandler<CheckedChangedEventArgs>? CheckedChanged;

    /// <summary>
    ///     Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        BackgroundColor = Color.White;
        BorderColor = new Color(118, 118, 118);
        CheckMarkColor = Color.Black;
        TextColor = Color.Black;
        DisabledTextColor = Color.Gray;
        HoverBackgroundColor = new Color(229, 241, 251);
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
    ///     Updates the component state
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled)
        {
            _isHovered = false;
            base.Update(gameTime);
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        // Check if mouse is over the checkbox
        var checkBoxBounds = GetCheckBoxBounds();
        _isHovered = checkBoxBounds.Contains(mousePosition);

        // Handle mouse clicks
        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            IsChecked = !IsChecked;
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Gets the bounds of the checkbox square
    /// </summary>
    private Rectangle GetCheckBoxBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)(Position.Y + (Size.Y - CheckBoxSize) / 2),
            CheckBoxSize,
            CheckBoxSize
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
        var checkBoxBounds = GetCheckBoxBounds();
        checkBoxBounds.X += (int)parentPosition.X;
        checkBoxBounds.Y += (int)parentPosition.Y;

        // Draw checkbox background
        var bgColor = IsEnabled && _isHovered ? HoverBackgroundColor : BackgroundColor;
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), checkBoxBounds, bgColor);

        // Draw checkbox border
        DrawBorder(spriteBatch, checkBoxBounds);

        // Draw check mark if checked
        if (IsChecked)
        {
            DrawCheckMark(spriteBatch, checkBoxBounds);
        }

        // Draw text
        if (!string.IsNullOrEmpty(Text))
        {
            var textColor = IsEnabled ? TextColor : DisabledTextColor;
            var textPosition = new Vector2(
                checkBoxBounds.Right + Spacing,
                position.Y + (Size.Y - _font.LineHeight) / 2
            );
            spriteBatch.DrawString(_font, Text, textPosition, textColor);
        }
    }

    /// <summary>
    ///     Draws the checkbox border
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var pixel = _assetManagerService.GetPixelTexture();
        if (pixel == null)
        {
            return;
        }

        // Top
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), BorderColor);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), BorderColor);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), BorderColor);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), BorderColor);
    }

    /// <summary>
    ///     Draws the check mark
    /// </summary>
    private void DrawCheckMark(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var pixel = _assetManagerService.GetPixelTexture();
        if (pixel == null)
        {
            return;
        }

        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var size = bounds.Width / 4;

        // Draw a simple check mark using lines
        // Top-left to bottom-right
        for (var i = -size; i <= size; i++)
        {
            var x = centerX + i;
            var y = centerY + i;
            if (x >= bounds.X + 2 && x < bounds.Right - 2 &&
                y >= bounds.Y + 2 && y < bounds.Bottom - 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, y, 1, 1), CheckMarkColor);
            }
        }

        // Top-right to bottom-left
        for (var i = -size; i <= size; i++)
        {
            var x = centerX - i;
            var y = centerY + i;
            if (x >= bounds.X + 2 && x < bounds.Right - 2 &&
                y >= bounds.Y + 2 && y < bounds.Bottom - 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, y, 1, 1), CheckMarkColor);
            }
        }
    }
}

/// <summary>
///     Event args for checked state changes
/// </summary>
public class CheckedChangedEventArgs : EventArgs
{
    public CheckedChangedEventArgs(bool isChecked)
    {
        IsChecked = isChecked;
    }

    public bool IsChecked { get; }
}