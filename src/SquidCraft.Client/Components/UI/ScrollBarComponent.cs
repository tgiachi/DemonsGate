using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     ScrollBar component for scrolling content
/// </summary>
public class ScrollBarComponent : BaseComponent
{
    private IAssetManagerService _assetManagerService;
    private MouseState _previousMouseState;
    private bool _isDragging;
    private float _value;
    private bool _isHovered;

    /// <summary>
    ///     Initializes a new ScrollBar component
    /// </summary>
    /// <param name="orientation">Orientation of the scrollbar</param>
    /// <param name="length">Length of the scrollbar</param>
    public ScrollBarComponent(ScrollBarOrientation orientation = ScrollBarOrientation.Vertical, float length = 200f)
    {
        Orientation = orientation;
        Size = orientation == ScrollBarOrientation.Vertical
            ? new Vector2(16, length)
            : new Vector2(length, 16);

        // Default styling
        SetDefaultColors();
    }

    /// <summary>
    ///     Gets or sets the orientation
    /// </summary>
    public ScrollBarOrientation Orientation { get; set; }

    /// <summary>
    ///     Gets or sets the current value
    /// </summary>
    public float Value
    {
        get => _value;
        set
        {
            var newValue = MathHelper.Clamp(value, Minimum, Maximum);
            if (Math.Abs(_value - newValue) > float.Epsilon)
            {
                _value = newValue;
                ValueChanged?.Invoke(this, new ScrollValueChangedEventArgs(newValue));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the minimum value
    /// </summary>
    public float Minimum { get; set; }

    /// <summary>
    ///     Gets or sets the maximum value
    /// </summary>
    public float Maximum { get; set; } = 100f;

    /// <summary>
    ///     Gets or sets the small change value (arrow buttons)
    /// </summary>
    public float SmallChange { get; set; } = 1f;

    /// <summary>
    ///     Gets or sets the large change value (page up/down)
    /// </summary>
    public float LargeChange { get; set; } = 10f;

    /// <summary>
    ///     Gets or sets whether the scrollbar is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to show arrow buttons
    /// </summary>
    public bool ShowArrows { get; set; } = true;

    /// <summary>
    ///     Size of the arrow buttons
    /// </summary>
    public int ArrowSize { get; set; } = 16;

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color ThumbColor { get; set; }
    public Color ThumbHoverColor { get; set; }
    public Color ThumbPressedColor { get; set; }
    public Color ArrowColor { get; set; }
    public Color ArrowHoverColor { get; set; }
    public Color BorderColor { get; set; }

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
    public event EventHandler<ScrollValueChangedEventArgs>? ValueChanged;

    /// <summary>
    ///     Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        BackgroundColor = new Color(240, 240, 240);
        ThumbColor = new Color(200, 200, 200);
        ThumbHoverColor = new Color(180, 180, 180);
        ThumbPressedColor = new Color(160, 160, 160);
        ArrowColor = new Color(180, 180, 180);
        ArrowHoverColor = new Color(160, 160, 160);
        BorderColor = new Color(118, 118, 118);
    }

    /// <summary>
    ///     Initializes the component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        _assetManagerService = SquidCraftClientContext.AssetManagerService;
        GraphicsDevice = SquidCraftClientContext.GraphicsDevice;
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
            _isDragging = false;
            base.Update(gameTime);
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        var thumbBounds = GetThumbBounds();
        var wasHovered = _isHovered;
        _isHovered = thumbBounds.Contains(mousePosition);

        // Handle mouse press/release
        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            _isDragging = true;
        }
        else if (_isDragging && mouseState.LeftButton == ButtonState.Released)
        {
            _isDragging = false;
        }

        // Handle dragging
        if (_isDragging && mouseState.LeftButton == ButtonState.Pressed)
        {
            UpdateValueFromMouse(mousePosition);
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Updates the value based on mouse position
    /// </summary>
    private void UpdateValueFromMouse(Vector2 mousePosition)
    {
        var trackBounds = GetTrackBounds();
        float percentage;

        if (Orientation == ScrollBarOrientation.Vertical)
        {
            percentage = (mousePosition.Y - trackBounds.Y) / trackBounds.Height;
        }
        else
        {
            percentage = (mousePosition.X - trackBounds.X) / trackBounds.Width;
        }

        percentage = MathHelper.Clamp(percentage, 0f, 1f);
        Value = Minimum + percentage * (Maximum - Minimum);
    }

    /// <summary>
    ///     Gets the bounds of the scroll track
    /// </summary>
    private Rectangle GetTrackBounds()
    {
        var arrowsOffset = ShowArrows ? ArrowSize : 0;

        if (Orientation == ScrollBarOrientation.Vertical)
        {
            return new Rectangle(
                (int)Position.X,
                (int)(Position.Y + arrowsOffset),
                (int)Size.X,
                (int)(Size.Y - 2 * arrowsOffset)
            );
        }
        else
        {
            return new Rectangle(
                (int)(Position.X + arrowsOffset),
                (int)Position.Y,
                (int)(Size.X - 2 * arrowsOffset),
                (int)Size.Y
            );
        }
    }

    /// <summary>
    ///     Gets the bounds of the scroll thumb
    /// </summary>
    private Rectangle GetThumbBounds()
    {
        var trackBounds = GetTrackBounds();
        var range = Maximum - Minimum;
        var thumbSize = range > 0 ? Math.Max(20, trackBounds.Width * 0.3f) : trackBounds.Width;

        if (Orientation == ScrollBarOrientation.Vertical)
        {
            thumbSize = Math.Min(thumbSize, trackBounds.Height);
            var thumbY = trackBounds.Y + (Value - Minimum) / range * (trackBounds.Height - thumbSize);
            return new Rectangle((int)trackBounds.X, (int)thumbY, (int)trackBounds.Width, (int)thumbSize);
        }
        else
        {
            thumbSize = Math.Min(thumbSize, trackBounds.Width);
            var thumbX = trackBounds.X + (Value - Minimum) / range * (trackBounds.Width - thumbSize);
            return new Rectangle((int)thumbX, (int)trackBounds.Y, (int)thumbSize, (int)trackBounds.Height);
        }
    }

    /// <summary>
    ///     Draws the component content
    /// </summary>
    /// <param name="spriteBatch">SpriteBatch for drawing</param>
    /// <param name="gameTime">Game timing information</param>
    /// <param name="parentPosition">Parent position offset</param>
    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (_assetManagerService.GetPixelTexture() == null)
        {
            return;
        }

        var position = Position + parentPosition;
        var bounds = new Rectangle((int)position.X, (int)position.Y, (int)Size.X, (int)Size.Y);

        // Draw background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), bounds, BackgroundColor);

        // Draw border
        DrawBorder(spriteBatch, bounds);

        // Draw arrows if enabled
        if (ShowArrows)
        {
            DrawArrows(spriteBatch, bounds);
        }

        // Draw track
        var trackBounds = GetTrackBounds();
        trackBounds.X += (int)parentPosition.X;
        trackBounds.Y += (int)parentPosition.Y;

        // Draw thumb
        var thumbBounds = GetThumbBounds();
        thumbBounds.X += (int)parentPosition.X;
        thumbBounds.Y += (int)parentPosition.Y;

        var thumbColor = _isDragging ? ThumbPressedColor :
                        _isHovered ? ThumbHoverColor : ThumbColor;
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), thumbBounds, thumbColor);
    }

    /// <summary>
    ///     Draws the scrollbar border
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
    ///     Draws the arrow buttons
    /// </summary>
    private void DrawArrows(SpriteBatch spriteBatch, Rectangle bounds)
    {
        if (Orientation == ScrollBarOrientation.Vertical)
        {
            // Up arrow
            var upArrowBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, ArrowSize);
            DrawArrow(spriteBatch, upArrowBounds, true);

            // Down arrow
            var downArrowBounds = new Rectangle(bounds.X, bounds.Bottom - ArrowSize, bounds.Width, ArrowSize);
            DrawArrow(spriteBatch, downArrowBounds, false);
        }
        else
        {
            // Left arrow
            var leftArrowBounds = new Rectangle(bounds.X, bounds.Y, ArrowSize, bounds.Height);
            DrawArrow(spriteBatch, leftArrowBounds, true);

            // Right arrow
            var rightArrowBounds = new Rectangle(bounds.Right - ArrowSize, bounds.Y, ArrowSize, bounds.Height);
            DrawArrow(spriteBatch, rightArrowBounds, false);
        }
    }

    /// <summary>
    ///     Draws an arrow button
    /// </summary>
    private void DrawArrow(SpriteBatch spriteBatch, Rectangle bounds, bool isUpOrLeft)
    {
        var pixel = _assetManagerService.GetPixelTexture();
        if (pixel == null)
        {
            return;
        }

        // Draw arrow background
        spriteBatch.Draw(pixel, bounds, ArrowColor);

        // Draw arrow symbol
        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var arrowSize = 3;

        if (Orientation == ScrollBarOrientation.Vertical)
        {
            if (isUpOrLeft)
            {
                // Up arrow
                for (var i = 0; i <= arrowSize; i++)
                {
                    var y = centerY - arrowSize / 2 + i;
                    var width = (arrowSize - i) * 2 + 1;
                    var x = centerX - width / 2;
                    spriteBatch.Draw(pixel, new Rectangle(x, y, width, 1), Color.Black);
                }
            }
            else
            {
                // Down arrow
                for (var i = 0; i <= arrowSize; i++)
                {
                    var y = centerY - arrowSize / 2 + i;
                    var width = i * 2 + 1;
                    var x = centerX - width / 2;
                    spriteBatch.Draw(pixel, new Rectangle(x, y, width, 1), Color.Black);
                }
            }
        }
        else
        {
            if (isUpOrLeft)
            {
                // Left arrow
                for (var i = 0; i <= arrowSize; i++)
                {
                    var x = centerX - arrowSize / 2 + i;
                    var height = (arrowSize - i) * 2 + 1;
                    var y = centerY - height / 2;
                    spriteBatch.Draw(pixel, new Rectangle(x, y, 1, height), Color.Black);
                }
            }
            else
            {
                // Right arrow
                for (var i = 0; i <= arrowSize; i++)
                {
                    var x = centerX - arrowSize / 2 + i;
                    var height = i * 2 + 1;
                    var y = centerY - height / 2;
                    spriteBatch.Draw(pixel, new Rectangle(x, y, 1, height), Color.Black);
                }
            }
        }
    }
}

/// <summary>
///     ScrollBar orientation
/// </summary>
public enum ScrollBarOrientation
{
    Vertical,
    Horizontal
}

/// <summary>
///     Event args for scroll value changes
/// </summary>
public class ScrollValueChangedEventArgs : EventArgs
{
    public ScrollValueChangedEventArgs(float value)
    {
        Value = value;
    }

    public float Value { get; }
}