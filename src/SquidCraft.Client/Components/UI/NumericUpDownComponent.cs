using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     NumericUpDown component for selecting numeric values with up/down buttons
/// </summary>
public class NumericUpDownComponent : BaseComponent
{
    private readonly int _repeatDelay = 500;    // Initial delay before repeating
    private readonly int _repeatInterval = 100; // Interval between repeats
    private IAssetManagerService _assetManagerService;
    private bool _isDownButtonHovered;
    private bool _isDownButtonPressed;
    private bool _isRepeating;
    private bool _isUpButtonHovered;
    private bool _isUpButtonPressed;

    private MouseState _previousMouseState;
    private double _repeatTimer;
    private bool _repeatUp; // True for up, false for down
    private Texture2D? _downButtonTexture;
    private FontStashSharp.SpriteFontBase? _font;
    private Texture2D? _upButtonTexture;
    private float _value;

    /// <summary>
    ///     Initializes a new NumericUpDown component
    /// </summary>
    /// <param name="minimum">Minimum value</param>
    /// <param name="maximum">Maximum value</param>
    /// <param name="initialValue">Initial value</param>
    /// <param name="width">Width of the component</param>
    public NumericUpDownComponent(
        float minimum = 0f, float maximum = 100f, float initialValue = 0f, float width = 120f
    )
    {
        _assetManagerService = SquidCraftClientContext.AssetManagerService;
        Minimum = minimum;
        Maximum = maximum;
        _value = Math.Clamp(initialValue, minimum, maximum);
        Size = new Vector2(width, 24);

        // Default styling
        SetDefaultColors();

        // Load resources
        LoadTextures();
        LoadFont();
    }

    private void LoadFont()
    {
        _font = _assetManagerService.GetFontTtf("DefaultFont", 14);
    }



    /// <summary>
    ///     Gets or sets the minimum value
    /// </summary>
    public float Minimum { get; set; }

    /// <summary>
    ///     Gets or sets the maximum value
    /// </summary>
    public float Maximum { get; set; }

    /// <summary>
    ///     Gets or sets the current value
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var newValue = Math.Clamp(value, Minimum, Maximum);
            if (Math.Abs(_value - newValue) > 0.001f)
            {
                _value = newValue;
                ValueChanged?.Invoke(this, _value);
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether the component is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets the step size for increments/decrements
    /// </summary>
    public float Step { get; set; } = 1f;

    /// <summary>
    ///     Gets or sets the number of decimal places to display
    /// </summary>
    public int DecimalPlaces { get; set; } = 0;

    /// <summary>
    ///     Width of the up/down buttons
    /// </summary>
    public float ButtonWidth { get; set; } = 16f;

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color DisabledBackgroundColor { get; set; }
    public Color DisabledBorderColor { get; set; }
    public Color TextColor { get; set; }
    public Color DisabledTextColor { get; set; }

    public Color ButtonNormalColor { get; set; }
    public Color ButtonHoverColor { get; set; }
    public Color ButtonPressedColor { get; set; }
    public Color ButtonDisabledColor { get; set; }

    /// <summary>
    ///     Gets or sets the font color for the value text
    /// </summary>
    public Color FontColor
    {
        get => TextColor;
        set => TextColor = value;
    }

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
    ///     Event fired when the value changes
    /// </summary>
    public event EventHandler<float>? ValueChanged;

    /// <summary>
    ///     Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        BackgroundColor = Color.White;
        BorderColor = new Color(118, 118, 118);
        DisabledBackgroundColor = new Color(245, 245, 245);
        DisabledBorderColor = new Color(204, 204, 204);
        TextColor = Color.Black;
        DisabledTextColor = Color.Gray;

        ButtonNormalColor = new Color(240, 240, 240);
        ButtonHoverColor = new Color(229, 241, 251);
        ButtonPressedColor = new Color(204, 228, 247);
        ButtonDisabledColor = new Color(245, 245, 245);
    }

    /// <summary>
    ///     Initializes the component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    ///     Loads the button textures
    /// </summary>
    private void LoadTextures()
    {
        // For simplicity, use programmatic drawing
        _upButtonTexture = null;
        _downButtonTexture = null;
    }

    /// <summary>
    ///     Updates the component state
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled)
        {
            _isUpButtonHovered = false;
            _isDownButtonHovered = false;
            _isUpButtonPressed = false;
            _isDownButtonPressed = false;
            _isRepeating = false;
            base.Update(gameTime);
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        var upButtonBounds = GetUpButtonBounds();
        var downButtonBounds = GetDownButtonBounds();

        // Update hover states
        _isUpButtonHovered = upButtonBounds.Contains(mousePosition);
        _isDownButtonHovered = downButtonBounds.Contains(mousePosition);

        // Handle mouse clicks
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            if (_isUpButtonHovered)
            {
                _isUpButtonPressed = true;
                IncrementValue();
                StartRepeat(true);
            }
            else if (_isDownButtonHovered)
            {
                _isDownButtonPressed = true;
                DecrementValue();
                StartRepeat(false);
            }
        }

        // Handle mouse release
        if (mouseState.LeftButton == ButtonState.Released)
        {
            _isUpButtonPressed = false;
            _isDownButtonPressed = false;
            StopRepeat();
        }

        // Handle repeat logic
        if (_isRepeating)
        {
            _repeatTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            var threshold = _repeatTimer < _repeatDelay ? _repeatDelay : _repeatInterval;

            if (_repeatTimer >= threshold)
            {
                if (_repeatUp)
                {
                    IncrementValue();
                }
                else
                {
                    DecrementValue();
                }

                _repeatTimer = _repeatDelay; // Reset to interval timing
            }
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Gets the bounds of the up button
    /// </summary>
    private Rectangle GetUpButtonBounds()
    {
        var position = Position;
        return new Rectangle(
            (int)(position.X + Size.X - ButtonWidth),
            (int)position.Y,
            (int)ButtonWidth,
            (int)(Size.Y / 2)
        );
    }

    /// <summary>
    ///     Gets the bounds of the down button
    /// </summary>
    private Rectangle GetDownButtonBounds()
    {
        var position = Position;
        return new Rectangle(
            (int)(position.X + Size.X - ButtonWidth),
            (int)(position.Y + Size.Y / 2),
            (int)ButtonWidth,
            (int)(Size.Y / 2)
        );
    }

    /// <summary>
    ///     Gets the bounds of the text area
    /// </summary>
    private Rectangle GetTextBounds()
    {
        var position = Position;
        return new Rectangle(
            (int)position.X,
            (int)position.Y,
            (int)(Size.X - ButtonWidth),
            (int)Size.Y
        );
    }

    /// <summary>
    ///     Starts the repeat timer
    /// </summary>
    private void StartRepeat(bool up)
    {
        _isRepeating = true;
        _repeatUp = up;
        _repeatTimer = 0;
    }

    /// <summary>
    ///     Stops the repeat timer
    /// </summary>
    private void StopRepeat()
    {
        _isRepeating = false;
        _repeatTimer = 0;
    }

    /// <summary>
    ///     Increments the value by the step amount
    /// </summary>
    public void IncrementValue()
    {
        Value = Math.Min(Maximum, _value + Step);
    }

    /// <summary>
    ///     Decrements the value by the step amount
    /// </summary>
    public void DecrementValue()
    {
        Value = Math.Max(Minimum, _value - Step);
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
        var textBounds = GetTextBounds();
        textBounds.X += (int)position.X;
        textBounds.Y += (int)position.Y;
        var upButtonBounds = GetUpButtonBounds();
        upButtonBounds.X += (int)position.X;
        upButtonBounds.Y += (int)position.Y;
        var downButtonBounds = GetDownButtonBounds();
        downButtonBounds.X += (int)position.X;
        downButtonBounds.Y += (int)position.Y;

        // Draw main background and border
        var bgColor = IsEnabled ? BackgroundColor : DisabledBackgroundColor;
        var borderColor = IsEnabled ? BorderColor : DisabledBorderColor;

        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), textBounds, bgColor);

        // Draw borders
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(textBounds.X, textBounds.Y, textBounds.Width, 1), borderColor);
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(textBounds.X, textBounds.Bottom - 1, textBounds.Width, 1), borderColor);
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(textBounds.X, textBounds.Y, 1, textBounds.Height), borderColor);
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(textBounds.Right - 1, textBounds.Y, 1, textBounds.Height), borderColor);

        // Draw value text
        var valueText = DecimalPlaces > 0 ? _value.ToString($"F{DecimalPlaces}") : _value.ToString("F0");
        var textColor = IsEnabled ? TextColor : DisabledTextColor;
        var textSize = _font.MeasureString(valueText);
        var textPosition = new Vector2(
            textBounds.X + (textBounds.Width - textSize.X) / 2,
            textBounds.Y + (textBounds.Height - textSize.Y) / 2
        );

        spriteBatch.DrawString(_font, valueText, textPosition, textColor);

        // Draw buttons
        DrawUpButton(spriteBatch, upButtonBounds);
        DrawDownButton(spriteBatch, downButtonBounds);
    }

    /// <summary>
    ///     Draws the up button
    /// </summary>
    private void DrawUpButton(SpriteBatch spriteBatch, Rectangle bounds)
    {
        // Draw button using texture if available, otherwise fall back to programmatic drawing
        if (_upButtonTexture != null)
        {
            var buttonColor = GetButtonColor(true);
            spriteBatch.Draw(_upButtonTexture, bounds, buttonColor);
        }
        else
        {
            DrawButtonFallback(spriteBatch, bounds, true);
        }
    }

    /// <summary>
    ///     Draws the down button
    /// </summary>
    private void DrawDownButton(SpriteBatch spriteBatch, Rectangle bounds)
    {
        // Draw button using texture if available, otherwise fall back to programmatic drawing
        if (_downButtonTexture != null)
        {
            var buttonColor = GetButtonColor(false);
            spriteBatch.Draw(_downButtonTexture, bounds, buttonColor);
        }
        else
        {
            DrawButtonFallback(spriteBatch, bounds, false);
        }
    }

    /// <summary>
    ///     Gets the button color based on state
    /// </summary>
    private Color GetButtonColor(bool isUpButton)
    {
        if (!IsEnabled)
        {
            return Color.Gray;
        }

        var isPressed = isUpButton ? _isUpButtonPressed : _isDownButtonPressed;
        var isHovered = isUpButton ? _isUpButtonHovered : _isDownButtonHovered;

        if (isPressed)
        {
            return Color.Lerp(Color.White, Color.Blue, 0.3f);
        }

        if (isHovered)
        {
            return Color.Lerp(Color.White, Color.LightBlue, 0.2f);
        }

        return Color.White;
    }

    /// <summary>
    ///     Draws button using programmatic drawing when texture is not available
    /// </summary>
    private void DrawButtonFallback(SpriteBatch spriteBatch, Rectangle bounds, bool isUpButton)
    {
        if (_assetManagerService.GetPixelTexture() == null)
        {
            return;
        }

        var isPressed = isUpButton ? _isUpButtonPressed : _isDownButtonPressed;
        var isHovered = isUpButton ? _isUpButtonHovered : _isDownButtonHovered;

        Color buttonColor;
        if (!IsEnabled)
        {
            buttonColor = ButtonDisabledColor;
        }
        else if (isPressed)
        {
            buttonColor = ButtonPressedColor;
        }
        else if (isHovered)
        {
            buttonColor = ButtonHoverColor;
        }
        else
        {
            buttonColor = ButtonNormalColor;
        }

        // Draw button background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), bounds, buttonColor);

        // Draw button border
        var borderColor = IsEnabled ? BorderColor : DisabledBorderColor;
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), borderColor);
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), borderColor);
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), borderColor);

        // Draw arrow
        var arrowColor = IsEnabled ? Color.Black : DisabledTextColor;
        DrawArrow(spriteBatch, bounds, isUpButton, arrowColor);
    }

    /// <summary>
    ///     Draws an up or down arrow
    /// </summary>
    private void DrawArrow(SpriteBatch spriteBatch, Rectangle bounds, bool isUpArrow, Color color)
    {
        if (_assetManagerService.GetPixelTexture() == null)
        {
            return;
        }

        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var arrowSize = 3;

        if (isUpArrow)
        {
            // Draw up arrow (triangle pointing up)
            for (var i = 0; i <= arrowSize; i++)
            {
                var y = centerY - arrowSize / 2 + i;
                var width = (arrowSize - i) * 2 + 1;
                var x = centerX - width / 2;
                spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(x, y, width, 1), color);
            }
        }
        else
        {
            // Draw down arrow (triangle pointing down)
            for (var i = 0; i <= arrowSize; i++)
            {
                var y = centerY - arrowSize / 2 + i;
                var width = i * 2 + 1;
                var x = centerX - width / 2;
                spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(x, y, width, 1), color);
            }
        }
    }

    /// <summary>
    ///     Sets the value without triggering events
    /// </summary>
    /// <param name="value">New value</param>
    public void SetValueSilent(float value)
    {
        _value = Math.Clamp(value, Minimum, Maximum);
    }

    /// <summary>
    ///     Disposes resources
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Pixel texture is managed by AssetManagerService
        }

        base.Dispose(disposing);
    }
}