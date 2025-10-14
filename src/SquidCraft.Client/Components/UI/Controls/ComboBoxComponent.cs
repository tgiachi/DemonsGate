using System;
using System.Collections.Generic;
using System.Linq;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Single-selection combo box with keyboard and mouse interaction.
/// </summary>
public class ComboBoxComponent : BaseComponent
{
    private readonly List<string> _items = new();
    private SpriteFontBase? _font;
    private float _itemHeight;
    private bool _isDropdownOpen;
    private bool _mousePressed;
    private KeyboardState _previousKeyboardState;
    private int _hoveredIndex = -1;
    private int _scrollOffset;
    private bool _autoSize = true;

    public ComboBoxComponent(
        IEnumerable<string>? items = null,
        string fontName = "DefaultFont",
        int fontSize = 16,
        Vector2? position = null
    )
    {
        FontName = fontName;
        FontSize = Math.Max(8, fontSize);
        Position = position ?? Vector2.Zero;

        BackgroundColor = new Color(45, 49, 54);
        BorderColor = new Color(75, 83, 92);
        FocusedBorderColor = new Color(116, 198, 255);
        DropdownBackgroundColor = new Color(33, 37, 41);
        ItemHoverColor = new Color(73, 80, 87);
        ItemSelectedColor = new Color(52, 58, 64);
        ForegroundColor = Color.White;
        PlaceholderColor = new Color(200, 200, 200, 180);
        DropdownShadowColor = new Color(0, 0, 0, 70);

        Padding = new Vector2(12f, 8f);
        DropdownOffset = new Vector2(0f, 4f);
        MaxVisibleItems = 6;
        HasFocus = true;

        LoadFont();

        if (items != null)
        {
            AddItems(items);
        }
    }

    public event EventHandler<int>? SelectedIndexChanged;

    public string FontName { get; }

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

    private int _fontSize;

    public IReadOnlyList<string> Items => _items;

    public string PlaceholderText { get; set; } = "Select…";

    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color FocusedBorderColor { get; set; }
    public Color DropdownBackgroundColor { get; set; }
    public Color DropdownShadowColor { get; set; }
    public Color ItemHoverColor { get; set; }
    public Color ItemSelectedColor { get; set; }
    public Color ForegroundColor { get; set; }
    public Color PlaceholderColor { get; set; }

    public Vector2 Padding { get; set; }
    public Vector2 DropdownOffset { get; set; }

    public int MaxVisibleItems
    {
        get => _maxVisibleItems;
        set => _maxVisibleItems = Math.Max(1, value);
    }

    private int _maxVisibleItems;

    public bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize != value)
            {
                _autoSize = value;
                UpdateLayout();
            }
        }
    }

    public float Width
    {
        get => Size.X;
        set
        {
            var normalizedWidth = Math.Max(1f, value);
            var height = Size.Y;
            if (height <= 0)
            {
                height = _itemHeight <= 0 ? (_font?.LineHeight ?? 20f) : _itemHeight;
            }

            Size = new Vector2(normalizedWidth, height);

            if (_dropdownWidth < 0)
            {
                _dropdownWidth = normalizedWidth;
            }
        }
    }


    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = Math.Clamp(value, -1, _items.Count - 1);
            if (_selectedIndex != clamped)
            {
                _selectedIndex = clamped;
                SelectedIndexChanged?.Invoke(this, _selectedIndex);
            }
        }
    }

    private int _selectedIndex = -1;

    public string? SelectedItem => SelectedIndex >= 0 && SelectedIndex < _items.Count ? _items[SelectedIndex] : null;

    public override Vector2 Size
    {
        get => base.Size;
        set
        {
            base.Size = value;
            _autoSize = false;
        }
    }

    public float DropdownWidth
    {
        get => _dropdownWidth;
        set => _dropdownWidth = value;
    }

    private float _dropdownWidth = -1f;

    public void AddItem(string item)
    {
        _items.Add(item);
        UpdateLayout();
    }

    public void AddItems(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            _items.Add(item);
        }

        UpdateLayout();
    }

    public bool RemoveItem(string item)
    {
        var index = _items.IndexOf(item);
        if (index == -1)
        {
            return false;
        }

        _items.RemoveAt(index);
        if (SelectedIndex >= _items.Count)
        {
            SelectedIndex = _items.Count - 1;
        }

        UpdateLayout();
        return true;
    }

    public void ClearItems()
    {
        _items.Clear();
        SelectedIndex = -1;
        _scrollOffset = 0;
        _hoveredIndex = -1;
        _isDropdownOpen = false;
        UpdateLayout();
    }

    public override void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        var comboBounds = Bounds;

        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            if (!_mousePressed)
            {
                _mousePressed = true;

                if (comboBounds.Contains(mousePosition))
                {
                    ToggleDropdown();
                }
                else if (_isDropdownOpen && GetDropdownBounds(comboBounds).Contains(mousePosition))
                {
                    var index = GetItemIndexFromPosition(mousePosition, comboBounds);
                    if (index >= 0 && index < _items.Count)
                    {
                        SelectedIndex = index;
                    }

                    CloseDropdown();
                }
                else
                {
                    CloseDropdown();
                    IsFocused = false;
                }
            }
        }
        else
        {
            _mousePressed = false;
        }

        if (_isDropdownOpen)
        {
            UpdateHover(mousePosition, comboBounds);
            HandleMouseWheel(mouseState);
        }

        base.HandleMouse(mouseState, gameTime);
    }

    public override void HandleKeyboard(KeyboardState keyboardState, GameTime gameTime)
    {
        if (!IsEnabled || !HasFocus || !IsFocused)
        {
            _previousKeyboardState = keyboardState;
            return;
        }

        bool KeyPressed(Keys key) => keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);

        if (KeyPressed(Keys.Space) || KeyPressed(Keys.Enter))
        {
            if (_isDropdownOpen)
            {
                CloseDropdown();
            }
            else
            {
                OpenDropdown();
            }
        }
        else if (KeyPressed(Keys.Escape))
        {
            CloseDropdown();
        }
        else if (keyboardState.IsKeyDown(Keys.Down) && !_previousKeyboardState.IsKeyDown(Keys.Down))
        {
            if (!_isDropdownOpen)
            {
                OpenDropdown();
            }

            if (_items.Count > 0)
            {
                SelectedIndex = SelectedIndex < 0 ? 0 : Math.Min(SelectedIndex + 1, _items.Count - 1);
                EnsureItemVisible(SelectedIndex);
            }
        }
        else if (keyboardState.IsKeyDown(Keys.Up) && !_previousKeyboardState.IsKeyDown(Keys.Up))
        {
            if (_items.Count > 0)
            {
                SelectedIndex = SelectedIndex <= 0 ? 0 : SelectedIndex - 1;
                EnsureItemVisible(SelectedIndex);
            }
        }

        _previousKeyboardState = keyboardState;

        base.HandleKeyboard(keyboardState, gameTime);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (_font == null)
        {
            return;
        }

        var absolute = Position + parentPosition;
        var resolvedSize = ResolveSize();
        var bounds = new Rectangle(
            (int)absolute.X,
            (int)absolute.Y,
            (int)resolvedSize.X,
            (int)resolvedSize.Y
        );

        var graphicsDevice = spriteBatch.GraphicsDevice;
        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();

        Rectangle dropdownBounds = default;
        bool dropdownVisible = _isDropdownOpen;
        Rectangle? previousScissor = null;

        if (dropdownVisible)
        {
            dropdownBounds = GetDropdownBounds(bounds);
            if (dropdownBounds.Width > 0 && dropdownBounds.Height > 0)
            {
                var currentScissor = graphicsDevice.ScissorRectangle;
                var expanded = Rectangle.Union(currentScissor, dropdownBounds);
                expanded = Rectangle.Intersect(expanded, graphicsDevice.Viewport.Bounds);

                if (expanded.Width > 0 && expanded.Height > 0 && expanded != currentScissor)
                {
                    previousScissor = currentScissor;
                    graphicsDevice.ScissorRectangle = expanded;
                }
            }
        }

        spriteBatch.Draw(pixel, bounds, BackgroundColor * Opacity);

        var borderColor = IsFocused ? FocusedBorderColor : BorderColor;
        DrawRectangleBorder(spriteBatch, pixel, bounds, borderColor * Opacity);

        DrawText(spriteBatch, absolute);
        DrawArrow(spriteBatch, bounds);

        if (dropdownVisible)
        {
            DrawDropdown(spriteBatch, dropdownBounds);
        }

        if (previousScissor.HasValue)
        {
            graphicsDevice.ScissorRectangle = previousScissor.Value;
        }
    }

    protected override void OnFocusChanged()
    {
        if (!IsFocused)
        {
            CloseDropdown();
        }
    }

    protected override void OnInputFocusChanged()
    {
        if (!HasFocus)
        {
            CloseDropdown();
        }
    }

    private void LoadFont()
    {
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf(FontName, _fontSize);
        _itemHeight = _font.LineHeight + Padding.Y;
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        if (!_autoSize || _font == null)
        {
            return;
        }

        var textWidth = _items.Count == 0
            ? _font.MeasureString(PlaceholderText).X
            : _items.Max(item => _font.MeasureString(item).X);

        var width = textWidth + Padding.X * 2f + 24f;
        var height = _itemHeight;
        base.Size = new Vector2(width, height);
    }

    private void ToggleDropdown()
    {
        if (_isDropdownOpen)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    private void OpenDropdown()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _isDropdownOpen = true;
        IsFocused = true;
        _hoveredIndex = SelectedIndex;
        _previousWheelValue = Mouse.GetState().ScrollWheelValue;
        EnsureItemVisible(SelectedIndex);
    }

    private void CloseDropdown()
    {
        _isDropdownOpen = false;
        _hoveredIndex = -1;
        _previousWheelValue = Mouse.GetState().ScrollWheelValue;
    }

    private void UpdateHover(Vector2 mousePosition, Rectangle comboBounds)
    {
        var dropdownBounds = GetDropdownBounds(comboBounds);
        if (!dropdownBounds.Contains(mousePosition))
        {
            _hoveredIndex = -1;
            return;
        }

        var index = GetItemIndexFromPosition(mousePosition, comboBounds);
        _hoveredIndex = index;
    }

    private void HandleMouseWheel(MouseState mouseState)
    {
        var wheelDelta = mouseState.ScrollWheelValue - _previousWheelValue;
        _previousWheelValue = mouseState.ScrollWheelValue;

        if (wheelDelta == 0)
        {
            return;
        }

        var direction = wheelDelta > 0 ? -1 : 1;
        var visibleCount = Math.Min(MaxVisibleItems, _items.Count);

        _scrollOffset = Math.Clamp(_scrollOffset + direction, 0, Math.Max(0, _items.Count - visibleCount));
    }

    private int _previousWheelValue;

    private Rectangle GetDropdownBounds(Rectangle comboBounds)
    {
        var width = _dropdownWidth > 0 ? _dropdownWidth : comboBounds.Width;
        var visibleCount = Math.Min(MaxVisibleItems, _items.Count);
        var height = visibleCount * _itemHeight;

        return new Rectangle(
            (int)(comboBounds.X + DropdownOffset.X),
            (int)(comboBounds.Bottom + DropdownOffset.Y),
            (int)Math.Ceiling(width),
            (int)Math.Ceiling(height)
        );
    }

    private int GetItemIndexFromPosition(Vector2 position, Rectangle comboBounds)
    {
        var dropdownTop = comboBounds.Bottom + DropdownOffset.Y;
        var relativeY = position.Y - dropdownTop;

        if (relativeY < 0)
        {
            return -1;
        }

        var index = (int)(relativeY / _itemHeight) + _scrollOffset;

        return index >= _items.Count ? -1 : index;
    }

    private void EnsureItemVisible(int index)
    {
        if (index < 0)
        {
            return;
        }

        var visibleCount = Math.Min(MaxVisibleItems, _items.Count);

        if (index < _scrollOffset)
        {
            _scrollOffset = index;
        }
        else if (index >= _scrollOffset + visibleCount)
        {
            _scrollOffset = index - visibleCount + 1;
        }
    }

    private void DrawDropdown(SpriteBatch spriteBatch, Rectangle dropdownBounds)
    {
        if (_font == null || dropdownBounds.Width <= 0 || dropdownBounds.Height <= 0)
        {
            return;
        }

        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();

        // shadow
        var shadowRect = new Rectangle(
            dropdownBounds.X + 2,
            dropdownBounds.Y + 2,
            dropdownBounds.Width,
            dropdownBounds.Height
        );
        spriteBatch.Draw(pixel, shadowRect, DropdownShadowColor * Opacity);

        spriteBatch.Draw(pixel, dropdownBounds, DropdownBackgroundColor * Opacity);
        DrawRectangleBorder(spriteBatch, pixel, dropdownBounds, BorderColor * Opacity);

        var visibleCount = Math.Min(MaxVisibleItems, _items.Count);

        for (var i = 0; i < visibleCount; i++)
        {
            var itemIndex = _scrollOffset + i;
            if (itemIndex >= _items.Count)
            {
                break;
            }

            var itemRect = new Rectangle(
                dropdownBounds.X,
                dropdownBounds.Y + (int)(_itemHeight * i),
                dropdownBounds.Width,
                (int)_itemHeight
            );

            Color? background = null;

            if (itemIndex == SelectedIndex)
            {
                background = ItemSelectedColor;
            }

            if (itemIndex == _hoveredIndex)
            {
                background = ItemHoverColor;
            }

            if (background.HasValue)
            {
                spriteBatch.Draw(pixel, itemRect, background.Value * Opacity);
            }

            var textPosition = new Vector2(
                itemRect.X + Padding.X,
                itemRect.Y + (itemRect.Height - _font.LineHeight) / 2f
            );

            spriteBatch.DrawString(_font, _items[itemIndex], textPosition, ForegroundColor * Opacity);
        }
    }

    private void DrawText(SpriteBatch spriteBatch, Vector2 absolutePosition)
    {
        if (_font == null)
        {
            return;
        }

        var text = SelectedItem ?? PlaceholderText;
        var color = SelectedItem == null ? PlaceholderColor : ForegroundColor;

        var textPosition = absolutePosition + Padding;
        spriteBatch.DrawString(_font, text, textPosition, color * Opacity);
    }

    private void DrawArrow(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();

        var arrowWidth = 12f;
        var arrowHeight = 6f;
        var arrowX = bounds.Right - Padding.X - arrowWidth;
        var arrowY = bounds.Center.Y - arrowHeight / 2f;

        var triPoints = new[]
        {
            new Vector2(arrowX, arrowY),
            new Vector2(arrowX + arrowWidth, arrowY),
            new Vector2(arrowX + arrowWidth / 2f, arrowY + arrowHeight)
        };

        DrawFilledTriangle(spriteBatch, pixel, triPoints, ForegroundColor * Opacity);
    }

    private static void DrawRectangleBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
    }

    private static void DrawFilledTriangle(
        SpriteBatch spriteBatch, Texture2D pixel, IReadOnlyList<Vector2> points, Color color
    )
    {
        var minX = (int)Math.Floor(points.Min(p => p.X));
        var maxX = (int)Math.Ceiling(points.Max(p => p.X));
        var minY = (int)Math.Floor(points.Min(p => p.Y));
        var maxY = (int)Math.Ceiling(points.Max(p => p.Y));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var pt = new Vector2(x + 0.5f, y + 0.5f);
                if (PointInTriangle(pt, points[0], points[1], points[2]))
                {
                    spriteBatch.Draw(pixel, new Rectangle(x, y, 1, 1), color);
                }
            }
        }
    }

    private static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float s = p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y;
        float t = p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y;

        if ((s < 0) != (t < 0))
        {
            return false;
        }

        var area = -p1.Y * p2.X + p0.Y * (p2.X - p1.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y;

        if (area < 0)
        {
            s = -s;
            t = -t;
            area = -area;
        }

        return s > 0 && t > 0 && (s + t) < area;
    }
}
