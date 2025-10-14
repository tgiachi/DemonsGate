using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Interactive push button component with hover/press visual states and click notifications.
/// </summary>
public class ButtonComponent : BaseComponent
{
    private SpriteFontBase? _font;
    private string _text;
    private int _fontSize;
    private KeyboardState _previousKeyboardState;

    private bool _isHovering;
    private bool _isPressed;
    private bool _autoSize = true;

    /// <summary>
    /// Fired when the button is activated either via mouse click or keyboard.
    /// </summary>
    public event EventHandler? Clicked;

    /// <summary>
    /// Initializes a new instance of <see cref="ButtonComponent"/>.
    /// </summary>
    /// <param name="text">Label displayed on the button.</param>
    /// <param name="fontName">Font asset name used for rendering the label.</param>
    /// <param name="fontSize">Font size used when drawing the label.</param>
    /// <param name="position">Initial top-left position.</param>
    public ButtonComponent(
        string text = "Button",
        string fontName = "DefaultFont",
        int fontSize = 16,
        Vector2? position = null)
    {
        _text = text ?? string.Empty;
        FontName = fontName;
        _fontSize = fontSize;

        Position = position ?? Vector2.Zero;
        ForegroundColor = Color.White;
        BackgroundColor = new Color(52, 58, 64);
        HoverBackgroundColor = new Color(73, 80, 87);
        PressedBackgroundColor = new Color(33, 37, 41);
        DisabledBackgroundColor = new Color(73, 80, 87) * 0.4f;
        BorderColor = new Color(15, 15, 15, 200);

        Padding = new Vector2(16f, 10f);
        BorderThickness = 1;

        HasFocus = true;
        LoadFont();
        UpdateAutoSize();
    }

    /// <summary>
    /// Gets or sets the font name used by the button label.
    /// </summary>
    public string FontName { get; }

    /// <summary>
    /// Gets or sets the font size for the label.
    /// </summary>
    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = Math.Max(8, value);
                LoadFont();
                UpdateAutoSize();
            }
        }
    }

    /// <summary>
    /// Button caption text.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            var newText = value ?? string.Empty;
            if (_text != newText)
            {
                _text = newText;
                UpdateAutoSize();
            }
        }
    }

    /// <summary>
    /// Horizontal and vertical padding applied around the text when autosizing.
    /// </summary>
    public Vector2 Padding { get; set; }

    /// <summary>
    /// Default text color displayed on the button.
    /// </summary>
    public Color ForegroundColor { get; set; }

    /// <summary>
    /// Background color rendered when the button is idle.
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Background color used while hovering with the mouse.
    /// </summary>
    public Color HoverBackgroundColor { get; set; }

    /// <summary>
    /// Background color used while the button is pressed.
    /// </summary>
    public Color PressedBackgroundColor { get; set; }

    /// <summary>
    /// Background color applied when the button is disabled.
    /// </summary>
    public Color DisabledBackgroundColor { get; set; }

    /// <summary>
    /// Border color used to outline the button.
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// Outline thickness in pixels; set to zero to hide the border.
    /// </summary>
    public int BorderThickness { get; set; }

    /// <summary>
    /// When true the component automatically resizes to fit its content.
    /// </summary>
    public bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize != value)
            {
                _autoSize = value;
                if (_autoSize)
                {
                    UpdateAutoSize();
                }
            }
        }
    }

    /// <summary>
    /// Raises the click event.
    /// </summary>
    protected virtual void OnClicked()
    {
        if (!IsEnabled)
        {
            return;
        }

        Clicked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnEnabledChanged()
    {
        if (!IsEnabled)
        {
            _isHovering = false;
            _isPressed = false;
        }
    }

    /// <inheritdoc />
    public override Vector2 Size
    {
        get => base.Size;
        set
        {
            _autoSize = false;
            base.Size = value;
        }
    }

    /// <inheritdoc />
    public override void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled)
        {
            _isHovering = false;
            _isPressed = false;
            return;
        }

        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        var isInside = Contains(mousePosition);

        _isHovering = isInside;

        if (mouseState.LeftButton == ButtonState.Pressed && isInside)
        {
            if (!_isPressed)
            {
                _isPressed = true;
                HasFocus = true;
            }
        }
        else if (mouseState.LeftButton == ButtonState.Released)
        {
            if (_isPressed && isInside)
            {
                OnClicked();
            }

            _isPressed = false;
        }

        base.HandleMouse(mouseState, gameTime);
    }

    /// <inheritdoc />
    public override void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus || !IsFocused)
        {
            _previousKeyboardState = keyboardState;
            return;
        }

        var isEnterPressed = keyboardState.IsKeyDown(Keys.Enter) && !_previousKeyboardState.IsKeyDown(Keys.Enter);
        var isSpacePressed = keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space);

        if (isEnterPressed || isSpacePressed)
        {
            OnClicked();
        }

        _previousKeyboardState = keyboardState;

        base.HandleKeyboard(keyboardState, gameTime);
    }

    /// <inheritdoc />
    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        var font = _font;
        if (font == null)
        {
            return;
        }

        var absolutePosition = Position + parentPosition;
        var resolvedSize = ResolveSize();
        var bounds = new Rectangle(
            (int)absolutePosition.X,
            (int)absolutePosition.Y,
            (int)resolvedSize.X,
            (int)resolvedSize.Y
        );

        var backgroundColor = DetermineBackgroundColor();
        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();

        spriteBatch.Draw(pixel, bounds, backgroundColor * Opacity);

        if (BorderThickness > 0)
        {
            DrawBorder(spriteBatch, pixel, bounds, BorderThickness, BorderColor * Opacity);
        }

        var textSize = font.MeasureString(_text);
        var textPosition = absolutePosition + (resolvedSize - textSize) / 2f;
        spriteBatch.DrawString(font, _text, textPosition, ForegroundColor * Opacity);
    }

    private Color DetermineBackgroundColor()
    {
        if (!IsEnabled)
        {
            return DisabledBackgroundColor;
        }

        if (_isPressed)
        {
            return PressedBackgroundColor;
        }

        if (_isHovering)
        {
            return HoverBackgroundColor;
        }

        return BackgroundColor;
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, int thickness, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
    }

    private void LoadFont()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf(FontName, _fontSize);
    }

    private void UpdateAutoSize()
    {
        if (!_autoSize)
        {
            return;
        }

        if (_font == null)
        {
            return;
        }

        var textSize = _font.MeasureString(_text);
        var targetSize = textSize + Padding * 2f;
        base.Size = targetSize;
    }
}
