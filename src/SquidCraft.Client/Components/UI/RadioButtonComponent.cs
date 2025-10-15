using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;
using System.Collections.Generic;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     RadioButton component for exclusive selection within a group
/// </summary>
public class RadioButtonComponent : BaseComponent
{
    private static readonly Dictionary<string, RadioButtonComponent?> _groupSelections = new();
    private IAssetManagerService _assetManagerService;
    private SpriteFontBase? _font;
    private MouseState _previousMouseState;
    private bool _isChecked;
    private bool _isHovered;

    /// <summary>
    ///     Initializes a new RadioButton component
    /// </summary>
    /// <param name="text">The text to display next to the radio button</param>
    /// <param name="groupName">The group name for exclusive selection</param>
    public RadioButtonComponent(string text = "", string groupName = "")
    {
        Text = text;
        GroupName = groupName;
        Size = new Vector2(100, 20);

        // Default styling
        SetDefaultColors();
    }

    /// <summary>
    ///     Gets or sets the text displayed next to the radio button
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    ///     Gets or sets the group name for exclusive selection
    /// </summary>
    public string GroupName { get; set; }

    /// <summary>
    ///     Gets or sets whether the radio button is checked
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                if (value)
                {
                    // Uncheck other radio buttons in the same group
                    if (!string.IsNullOrEmpty(GroupName))
                    {
                        if (_groupSelections.TryGetValue(GroupName, out var currentSelected) &&
                            currentSelected != null && currentSelected != this)
                        {
                            currentSelected._isChecked = false;
                            currentSelected.CheckedChanged?.Invoke(currentSelected, new CheckedChangedEventArgs(false));
                        }
                        _groupSelections[GroupName] = this;
                    }
                }
                else if (!string.IsNullOrEmpty(GroupName) && _groupSelections.TryGetValue(GroupName, out var current) && current == this)
                {
                    _groupSelections[GroupName] = null;
                }

                _isChecked = value;
                CheckedChanged?.Invoke(this, new CheckedChangedEventArgs(value));
            }
        }
    }

    /// <summary>
    ///     Gets or sets whether the radio button is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Size of the radio button circle
    /// </summary>
    public int RadioButtonSize { get; set; } = 16;

    /// <summary>
    ///     Spacing between radio button and text
    /// </summary>
    public float Spacing { get; set; } = 4f;

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color DotColor { get; set; }
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
        DotColor = Color.Black;
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

        // Check if mouse is over the radio button
        var radioBounds = GetRadioButtonBounds();
        _isHovered = radioBounds.Contains(mousePosition);

        // Handle mouse clicks
        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed &&
            _previousMouseState.LeftButton == ButtonState.Released && !IsChecked)
        {
            IsChecked = true;
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Gets the bounds of the radio button circle
    /// </summary>
    private Rectangle GetRadioButtonBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)(Position.Y + (Size.Y - RadioButtonSize) / 2),
            RadioButtonSize,
            RadioButtonSize
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
        var radioBounds = GetRadioButtonBounds();
        radioBounds.X += (int)parentPosition.X;
        radioBounds.Y += (int)parentPosition.Y;

        // Draw radio button background
        var bgColor = IsEnabled && _isHovered ? HoverBackgroundColor : BackgroundColor;
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), radioBounds, bgColor);

        // Draw radio button border (circle)
        DrawCircleBorder(spriteBatch, radioBounds);

        // Draw dot if checked
        if (IsChecked)
        {
            DrawDot(spriteBatch, radioBounds);
        }

        // Draw text
        if (!string.IsNullOrEmpty(Text))
        {
            var textColor = IsEnabled ? TextColor : DisabledTextColor;
            var textPosition = new Vector2(
                radioBounds.Right + Spacing,
                position.Y + (Size.Y - _font.LineHeight) / 2
            );
            spriteBatch.DrawString(_font, Text, textPosition, textColor);
        }
    }

    /// <summary>
    ///     Draws the radio button border (circle)
    /// </summary>
    private void DrawCircleBorder(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var pixel = _assetManagerService.GetPixelTexture();
        if (pixel == null)
        {
            return;
        }

        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var radius = bounds.Width / 2;

        // Draw circle border using pixels
        for (var angle = 0; angle < 360; angle += 5)
        {
            var radian = MathHelper.ToRadians(angle);
            var x = centerX + (int)(MathF.Cos(radian) * radius);
            var y = centerY + (int)(MathF.Sin(radian) * radius);

            if (x >= bounds.X && x < bounds.Right && y >= bounds.Y && y < bounds.Bottom)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, y, 1, 1), BorderColor);
            }
        }
    }

    /// <summary>
    ///     Draws the dot when checked
    /// </summary>
    private void DrawDot(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var pixel = _assetManagerService.GetPixelTexture();
        if (pixel == null)
        {
            return;
        }

        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var dotRadius = bounds.Width / 4;

        // Draw filled circle for the dot
        for (var x = -dotRadius; x <= dotRadius; x++)
        {
            for (var y = -dotRadius; y <= dotRadius; y++)
            {
                if (x * x + y * y <= dotRadius * dotRadius)
                {
                    var drawX = centerX + x;
                    var drawY = centerY + y;

                    if (drawX >= bounds.X + 2 && drawX < bounds.Right - 2 &&
                        drawY >= bounds.Y + 2 && drawY < bounds.Bottom - 2)
                    {
                        spriteBatch.Draw(pixel, new Rectangle(drawX, drawY, 1, 1), DotColor);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Gets the currently selected radio button in a group
    /// </summary>
    /// <param name="groupName">The group name</param>
    /// <returns>The selected radio button or null</returns>
    public static RadioButtonComponent? GetSelectedInGroup(string groupName)
    {
        return _groupSelections.TryGetValue(groupName, out var selected) ? selected : null;
    }

    /// <summary>
    ///     Clears the selection in a group
    /// </summary>
    /// <param name="groupName">The group name</param>
    public static void ClearGroupSelection(string groupName)
    {
        if (_groupSelections.TryGetValue(groupName, out var current) && current != null)
        {
            current._isChecked = false;
            current.CheckedChanged?.Invoke(current, new CheckedChangedEventArgs(false));
        }
        _groupSelections[groupName] = null;
    }
}