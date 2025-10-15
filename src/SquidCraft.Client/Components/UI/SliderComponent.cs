using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     Slider component for selecting numeric values within a range
/// </summary>
public class SliderComponent : BaseComponent
{
    private readonly string _fontName;
    private SquidCraft.Client.Interfaces.IAssetManagerService _assetManagerService;
    private SpriteFontBase? _font;
    private bool _isDragging;
    private bool _isHovered;

    private float _maxValue = 100f;
    private float _minValue;
    private MouseState _previousMouseState;
    private float _step = 1f;
    private Texture2D? _thumbTexture;
    private Texture2D? _trackTexture;
    private float _value = 50f;

    /// <summary>
    ///     Initializes a new Slider component
    /// </summary>
    /// <param name="minValue">Minimum value</param>
    /// <param name="maxValue">Maximum value</param>
    /// <param name="initialValue">Initial value</param>
    /// <param name="width">Width of the slider</param>
    /// <param name="fontName">Font name for value label</param>
    /// <param name="fontSize">Font size for value label</param>
    public SliderComponent(
        float minValue = 0f, float maxValue = 100f, float initialValue = 50f, float width = 200f,
        string fontName = "DefaultFont", int fontSize = 12
    )
    {
        _minValue = minValue;
        _maxValue = maxValue;
        _value = Math.Clamp(initialValue, minValue, maxValue);
        _fontName = fontName;
        FontSize = fontSize;

        // Default styling
        SetDefaultColors();
        SetDefaultSize(width);

        // Load resources
        LoadTextures();
        RecalculateSize();
    }

    /// <summary>
    ///     Gets or sets the minimum value
    /// </summary>
    public float MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            if (_maxValue < _minValue)
            {
                _maxValue = _minValue;
            }

            Value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    ///     Gets or sets the maximum value
    /// </summary>
    public float MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            if (_minValue > _maxValue)
            {
                _minValue = _maxValue;
            }

            Value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    ///     Gets or sets the current value
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var newValue = Math.Clamp(value, _minValue, _maxValue);

            // Apply step if specified
            if (_step > 0)
            {
                newValue = (float)(Math.Round((newValue - _minValue) / _step) * _step + _minValue);
                newValue = Math.Clamp(newValue, _minValue, _maxValue);
            }

            if (Math.Abs(_value - newValue) > float.Epsilon)
            {
                var oldValue = _value;
                _value = newValue;
                ValueChanged?.Invoke(this, new SliderValueChangedEventArgs(oldValue, newValue));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the step value for discrete increments
    /// </summary>
    public float Step
    {
        get => _step;
        set => _step = Math.Max(0f, value);
    }

    /// <summary>
    ///     Gets or sets whether the slider is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to show the value label
    /// </summary>
    public bool ShowValueLabel { get; set; } = true;

    /// <summary>
    ///     Gets or sets the slider orientation
    /// </summary>
    public SliderOrientation Orientation { get; set; } = SliderOrientation.Horizontal;

    /// <summary>
    ///     Font size for value label
    /// </summary>
    public int FontSize { get; set; }

    /// <summary>
    ///     Height of the slider track
    /// </summary>
    public float TrackHeight { get; set; } = 8f;

    /// <summary>
    ///     Size of the slider thumb
    /// </summary>
    public float ThumbSize { get; set; } = 20f;

    /// <summary>
    ///     Spacing between slider and value label
    /// </summary>
    public float LabelSpacing { get; set; } = 8f;

    /// <summary>
    ///     Value label format string
    /// </summary>
    public string ValueFormat { get; set; } = "F1";

    // Color properties for different states
    public Color TrackBackgroundColor { get; set; }
    public Color TrackFillColor { get; set; }
    public Color DisabledTrackColor { get; set; }

    public Color ThumbNormalColor { get; set; }
    public Color ThumbHoverColor { get; set; }
    public Color ThumbPressedColor { get; set; }
    public Color ThumbDisabledColor { get; set; }

    public Color ThumbBorderColor { get; set; }
    public Color ThumbBorderHoverColor { get; set; }
    public Color ThumbBorderPressedColor { get; set; }

    public Color ValueLabelColor { get; set; }
    public Color DisabledValueLabelColor { get; set; }

    /// <summary>
    ///     Gets or sets the font color for the value label
    /// </summary>
    public Color FontColor
    {
        get => ValueLabelColor;
        set => ValueLabelColor = value;
    }

    /// <summary>
    ///     Event fired when the value changes
    /// </summary>
    public event EventHandler<SliderValueChangedEventArgs>? ValueChanged;

    /// <summary>
    ///     Event fired when dragging starts
    /// </summary>
    public event EventHandler? DragStarted;

    /// <summary>
    ///     Event fired when dragging ends
    /// </summary>
    public event EventHandler? DragEnded;

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
        // Track colors
        TrackBackgroundColor = new Color(200, 200, 200);
        TrackFillColor = new Color(0, 120, 215);
        DisabledTrackColor = new Color(230, 230, 230);

        // Thumb colors
        ThumbNormalColor = Color.White;
        ThumbHoverColor = new Color(248, 248, 248);
        ThumbPressedColor = new Color(240, 240, 240);
        ThumbDisabledColor = new Color(245, 245, 245);

        // Thumb border colors
        ThumbBorderColor = new Color(118, 118, 118);
        ThumbBorderHoverColor = new Color(0, 120, 215);
        ThumbBorderPressedColor = new Color(0, 84, 153);

        // Label colors
        ValueLabelColor = Color.Black;
        DisabledValueLabelColor = Color.Gray;
    }

    /// <summary>
    ///     Sets default size based on orientation and parameters
    /// </summary>
    private void SetDefaultSize(float width)
    {
        if (Orientation == SliderOrientation.Horizontal)
        {
            var height = Math.Max(ThumbSize, TrackHeight);
            if (ShowValueLabel)
            {
                height += FontSize + LabelSpacing;
            }

            Size = new Vector2(width, height);
        }
        else
        {
            var width2 = Math.Max(ThumbSize, TrackHeight);
            if (ShowValueLabel)
            {
                width2 += 50 + LabelSpacing; // Approximate label width
            }

            Size = new Vector2(width2, width); // Swap for vertical
        }
    }

    /// <summary>
    ///     Initializes the component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _assetManagerService = SquidCraftClientContext.AssetManagerService;

        // Load font
        _font = _assetManagerService.GetFontTtf(_fontName, FontSize);

        // Assume AssetManagerService is available, but for simplicity, use programmatic drawing
        LoadTextures();

        // Recalculate size with loaded font
        RecalculateSize();
    }

    /// <summary>
    ///     Loads the slider textures
    /// </summary>
    private void LoadTextures()
    {
        // For simplicity, use programmatic drawing
        _trackTexture = null;
        _thumbTexture = null;
    }

    /// <summary>
    ///     Recalculates component size based on orientation and label
    /// </summary>
    private void RecalculateSize()
    {
        // Size is already set
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsEnabled)
        {
            _isHovered = false;
            _isDragging = false;
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        // Check if mouse is over the slider
        var sliderBounds = GetSliderBounds();
        var thumbBounds = GetThumbBounds();
        var wasHovered = _isHovered;

        _isHovered = sliderBounds.Contains(mousePosition) || thumbBounds.Contains(mousePosition);

        // Handle mouse press/release
        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            _isDragging = true;
            UpdateValueFromMouse(mousePosition);
            DragStarted?.Invoke(this, EventArgs.Empty);
        }
        else if (_isDragging && mouseState.LeftButton == ButtonState.Released)
        {
            _isDragging = false;
            DragEnded?.Invoke(this, EventArgs.Empty);
        }

        // Handle dragging
        if (_isDragging && mouseState.LeftButton == ButtonState.Pressed)
        {
            UpdateValueFromMouse(mousePosition);
        }

        _previousMouseState = mouseState;
    }

    /// <summary>
    ///     Updates the value based on mouse position
    /// </summary>
    private void UpdateValueFromMouse(Vector2 mousePosition)
    {
        var sliderBounds = GetSliderBounds();
        float percentage;

        if (Orientation == SliderOrientation.Horizontal)
        {
            percentage = (mousePosition.X - sliderBounds.X) / sliderBounds.Width;
        }
        else
        {
            percentage = 1f - (mousePosition.Y - sliderBounds.Y) / sliderBounds.Height;
        }

        percentage = Math.Clamp(percentage, 0f, 1f);
        Value = _minValue + percentage * (_maxValue - _minValue);
    }

    /// <summary>
    ///     Gets the bounds of the slider track
    /// </summary>
    private Rectangle GetSliderBounds()
    {
        var position = Position;

        if (Orientation == SliderOrientation.Horizontal)
        {
            var trackY = position.Y + (Size.Y - TrackHeight) / 2;
            if (ShowValueLabel)
            {
                trackY -= (FontSize + LabelSpacing) / 2;
            }

            return new Rectangle(
                (int)(position.X + ThumbSize / 2),
                (int)trackY,
                (int)(Size.X - ThumbSize),
                (int)TrackHeight
            );
        }

        var trackX = position.X + (Size.X - TrackHeight) / 2;
        if (ShowValueLabel)
        {
            trackX -= (50 + LabelSpacing) / 2; // Approximate
        }

        return new Rectangle(
            (int)trackX,
            (int)(position.Y + ThumbSize / 2),
            (int)TrackHeight,
            (int)(Size.Y - ThumbSize)
        );
    }

    /// <summary>
    ///     Gets the bounds of the slider thumb
    /// </summary>
    private Rectangle GetThumbBounds()
    {
        var sliderBounds = GetSliderBounds();
        var percentage = (_value - _minValue) / (_maxValue - _minValue);

        if (Orientation == SliderOrientation.Horizontal)
        {
            var thumbX = sliderBounds.X + percentage * sliderBounds.Width - ThumbSize / 2;
            var thumbY = sliderBounds.Y + sliderBounds.Height / 2 - ThumbSize / 2;
            return new Rectangle((int)thumbX, (int)thumbY, (int)ThumbSize, (int)ThumbSize);
        }
        else
        {
            var thumbX = sliderBounds.X + sliderBounds.Width / 2 - ThumbSize / 2;
            var thumbY = sliderBounds.Y + (1f - percentage) * sliderBounds.Height - ThumbSize / 2;
            return new Rectangle((int)thumbX, (int)thumbY, (int)ThumbSize, (int)ThumbSize);
        }
    }

    /// <summary>
    ///     Gets the current visual state of the thumb
    /// </summary>
    private UIButtonState GetThumbState()
    {
        if (!IsEnabled)
        {
            return UIButtonState.Disabled;
        }

        if (_isDragging)
        {
            return UIButtonState.Pressed;
        }

        if (_isHovered)
        {
            return UIButtonState.Hovered;
        }

        return UIButtonState.Normal;
    }

    /// <summary>
    ///     Gets the thumb color for the current state
    /// </summary>
    private Color GetThumbColor()
    {
        return GetThumbState() switch
        {
            UIButtonState.Normal   => ThumbNormalColor,
            UIButtonState.Hovered  => ThumbHoverColor,
            UIButtonState.Pressed  => ThumbPressedColor,
            UIButtonState.Disabled => ThumbDisabledColor,
            _                      => ThumbNormalColor
        };
    }

    /// <summary>
    ///     Gets the thumb border color for the current state
    /// </summary>
    private Color GetThumbBorderColor()
    {
        return GetThumbState() switch
        {
            UIButtonState.Normal   => ThumbBorderColor,
            UIButtonState.Hovered  => ThumbBorderHoverColor,
            UIButtonState.Pressed  => ThumbBorderPressedColor,
            UIButtonState.Disabled => ThumbBorderColor,
            _                      => ThumbBorderColor
        };
    }

    /// <summary>
    ///     Draws the component content
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="parentPosition">Parent position offset</param>
    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        DrawTrack(spriteBatch);
        DrawThumb(spriteBatch);

        if (ShowValueLabel)
        {
            DrawValueLabel(spriteBatch);
        }
    }

    /// <summary>
    ///     Draws the slider track
    /// </summary>
    private void DrawTrack(SpriteBatch spriteBatch)
    {
        if (_assetManagerService.GetPixelTexture() == null)
        {
            return;
        }

        var trackBounds = GetSliderBounds();
        var trackColor = IsEnabled ? TrackBackgroundColor : DisabledTrackColor;

        // Draw background track
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), trackBounds, trackColor);

        // Draw filled portion
        if (IsEnabled && _value > _minValue)
        {
            var percentage = (_value - _minValue) / (_maxValue - _minValue);
            Rectangle fillBounds;

            if (Orientation == SliderOrientation.Horizontal)
            {
                fillBounds = new Rectangle(
                    trackBounds.X,
                    trackBounds.Y,
                    (int)(trackBounds.Width * percentage),
                    trackBounds.Height
                );
            }
            else
            {
                var fillHeight = (int)(trackBounds.Height * percentage);
                fillBounds = new Rectangle(
                    trackBounds.X,
                    trackBounds.Bottom - fillHeight,
                    trackBounds.Width,
                    fillHeight
                );
            }

            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), fillBounds, TrackFillColor);
        }
    }

    /// <summary>
    ///     Draws the slider thumb
    /// </summary>
    private void DrawThumb(SpriteBatch spriteBatch)
    {
        var thumbBounds = GetThumbBounds();

        // Draw thumb using texture if available, otherwise fall back to programmatic drawing
        if (_thumbTexture != null)
        {
            var thumbColor = IsEnabled ? Color.White : Color.Gray;

            // Apply hover/pressed effects with slight tinting
            if (IsEnabled && _isDragging)
            {
                thumbColor = Color.Lerp(Color.White, Color.Blue, 0.3f);
            }
            else if (IsEnabled && _isHovered)
            {
                thumbColor = Color.Lerp(Color.White, Color.LightBlue, 0.2f);
            }

            spriteBatch.Draw(_thumbTexture, thumbBounds, thumbColor);
        }
        else
        {
            // Fallback to programmatic drawing
            DrawThumbFallback(spriteBatch, thumbBounds);
        }
    }

    /// <summary>
    ///     Draws thumb using programmatic drawing when texture is not available
    /// </summary>
    private void DrawThumbFallback(SpriteBatch spriteBatch, Rectangle thumbBounds)
    {
        if (_assetManagerService.GetPixelTexture() == null)
        {
            return;
        }

        // Draw thumb background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), thumbBounds, GetThumbColor());

        // Draw thumb border
        var borderColor = GetThumbBorderColor();
        var borderWidth = 2;

        // Top border
        spriteBatch.Draw(
            _assetManagerService.GetPixelTexture(),
            new Rectangle(thumbBounds.X, thumbBounds.Y, thumbBounds.Width, borderWidth),
            borderColor
        );
        // Bottom border
        spriteBatch.Draw(
            _assetManagerService.GetPixelTexture(),
            new Rectangle(thumbBounds.X, thumbBounds.Bottom - borderWidth, thumbBounds.Width, borderWidth),
            borderColor
        );
        // Left border
        spriteBatch.Draw(
            _assetManagerService.GetPixelTexture(),
            new Rectangle(thumbBounds.X, thumbBounds.Y, borderWidth, thumbBounds.Height),
            borderColor
        );
        // Right border
        spriteBatch.Draw(
            _assetManagerService.GetPixelTexture(),
            new Rectangle(thumbBounds.Right - borderWidth, thumbBounds.Y, borderWidth, thumbBounds.Height),
            borderColor
        );
    }

    /// <summary>
    ///     Draws the value label
    /// </summary>
    private void DrawValueLabel(SpriteBatch spriteBatch)
    {
        if (_font == null)
        {
            return;
        }

        var valueText = _value.ToString(ValueFormat);
        var textColor = IsEnabled ? ValueLabelColor : DisabledValueLabelColor;
        var sliderBounds = GetSliderBounds();

        Vector2 labelPosition;

        if (Orientation == SliderOrientation.Horizontal)
        {
            // Position label below the slider
            labelPosition = new Vector2(
                sliderBounds.X + sliderBounds.Width / 2 - _font.MeasureString(valueText).X / 2,
                sliderBounds.Bottom + LabelSpacing
            );
        }
        else
        {
            // Position label to the right of the slider
            labelPosition = new Vector2(
                sliderBounds.Right + LabelSpacing,
                sliderBounds.Y + sliderBounds.Height / 2 - _font.MeasureString(valueText).Y / 2
            );
        }

        spriteBatch.DrawString(_font, valueText, labelPosition, textColor);
    }

    /// <summary>
    ///     Sets the value without triggering events
    /// </summary>
    /// <param name="value">New value</param>
    public void SetValueSilent(float value)
    {
        _value = Math.Clamp(value, _minValue, _maxValue);

        // Apply step if specified
        if (_step > 0)
        {
            _value = (float)(Math.Round((_value - _minValue) / _step) * _step + _minValue);
            _value = Math.Clamp(_value, _minValue, _maxValue);
        }
    }

    /// <summary>
    ///     Gets the value as a percentage (0.0 to 1.0)
    /// </summary>
    public float GetPercentage()
    {
        return (_value - _minValue) / (_maxValue - _minValue);
    }

    /// <summary>
    ///     Sets the value from a percentage (0.0 to 1.0)
    /// </summary>
    /// <param name="percentage">Percentage value</param>
    public void SetFromPercentage(float percentage)
    {
        percentage = Math.Clamp(percentage, 0f, 1f);
        Value = _minValue + percentage * (_maxValue - _minValue);
    }

    /// <summary>
    ///     Disposes resources
    /// </summary>
    public void Dispose()
    {
        // Pixel texture is managed by AssetManagerService
    }
}

/// <summary>
///     Slider orientation
/// </summary>
public enum SliderOrientation
{
    Horizontal,
    Vertical
}

/// <summary>
///     Button state
/// </summary>
public enum UIButtonState
{
    Normal,
    Hovered,
    Pressed,
    Disabled
}

/// <summary>
///     Event args for slider value changes
/// </summary>
public class SliderValueChangedEventArgs : EventArgs
{
    public SliderValueChangedEventArgs(float oldValue, float newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public float OldValue { get; }
    public float NewValue { get; }
}