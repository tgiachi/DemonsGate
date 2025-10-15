using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;
using System.Collections.ObjectModel;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     ListBox component for displaying and selecting items from a list
/// </summary>
public class ListBoxComponent : BaseComponent
{
    private int _itemHeight = 20;
    private IAssetManagerService _assetManagerService;
    private SpriteFontBase? _font;
    private MouseState _previousMouseState;
    private int _selectedIndex = -1;

    /// <summary>
    ///     Initializes a new ListBox component
    /// </summary>
    /// <param name="width">Width of the list box</param>
    /// <param name="height">Height of the list box</param>
    public ListBoxComponent(float width = 200f, float height = 100f)
    {
        Items = new ObservableCollection<string>();
        Size = new Vector2(width, height);

        // Default styling
        SetDefaultColors();
    }

    /// <summary>
    ///     Gets the collection of items in the list box
    /// </summary>
    public ObservableCollection<string> Items { get; }

    /// <summary>
    ///     Gets or sets the selected index
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= -1 && value < Items.Count)
            {
                var oldIndex = _selectedIndex;
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, new SelectedIndexChangedEventArgs(oldIndex, value));
            }
        }
    }

    /// <summary>
    ///     Gets the selected item
    /// </summary>
    public string? SelectedItem => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

    /// <summary>
    ///     Gets or sets whether the list box is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Height of each item
    /// </summary>
    public int ItemHeight
    {
        get => _itemHeight;
        set => _itemHeight = Math.Max(10, value);
    }

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color ItemNormalColor { get; set; }
    public Color ItemHoverColor { get; set; }
    public Color ItemSelectedColor { get; set; }
    public Color TextColor { get; set; }
    public Color SelectedTextColor { get; set; }

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
    ///     Event fired when the selected index changes
    /// </summary>
    public event EventHandler<SelectedIndexChangedEventArgs>? SelectedIndexChanged;

    /// <summary>
    ///     Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        BackgroundColor = Color.White;
        BorderColor = new Color(118, 118, 118);
        ItemNormalColor = Color.White;
        ItemHoverColor = new Color(229, 241, 251);
        ItemSelectedColor = new Color(0, 120, 215);
        TextColor = Color.Black;
        SelectedTextColor = Color.White;
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
            base.Update(gameTime);
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        // Handle mouse clicks
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            var bounds = new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y);
            if (bounds.Contains(mousePosition))
            {
                var relativeY = mousePosition.Y - Position.Y;
                var itemIndex = (int)(relativeY / ItemHeight);
                if (itemIndex >= 0 && itemIndex < Items.Count)
                {
                    SelectedIndex = itemIndex;
                }
            }
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
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
        var bounds = new Rectangle((int)position.X, (int)position.Y, (int)Size.X, (int)Size.Y);

        // Draw background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), bounds, BackgroundColor);

        // Draw border
        DrawBorder(spriteBatch, bounds);

        // Draw items
        DrawItems(spriteBatch, bounds);
    }

    /// <summary>
    ///     Draws the border
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
    ///     Draws the items
    /// </summary>
    private void DrawItems(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        for (var i = 0; i < Items.Count; i++)
        {
            var itemY = bounds.Y + i * ItemHeight;
            if (itemY + ItemHeight < bounds.Y || itemY > bounds.Bottom)
            {
                continue; // Skip items outside visible area
            }

            var itemBounds = new Rectangle(bounds.X, itemY, bounds.Width, ItemHeight);
            var isSelected = i == _selectedIndex;
            var isHovered = itemBounds.Contains(mousePosition) && IsEnabled;

            // Determine item color
            Color itemColor;
            if (isSelected)
            {
                itemColor = ItemSelectedColor;
            }
            else if (isHovered)
            {
                itemColor = ItemHoverColor;
            }
            else
            {
                itemColor = ItemNormalColor;
            }

            // Draw item background
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), itemBounds, itemColor);

            // Draw item text
            var textColor = isSelected ? SelectedTextColor : TextColor;
            var textPosition = new Vector2(bounds.X + 4, itemY + (ItemHeight - _font.LineHeight) / 2f);
            spriteBatch.DrawString(_font, Items[i], textPosition, textColor);
        }
    }

    /// <summary>
    ///     Adds an item to the list
    /// </summary>
    /// <param name="item">Item to add</param>
    public void AddItem(string item)
    {
        Items.Add(item);
    }

    /// <summary>
    ///     Removes an item from the list
    /// </summary>
    /// <param name="item">Item to remove</param>
    public void RemoveItem(string item)
    {
        Items.Remove(item);
    }

    /// <summary>
    ///     Clears all items
    /// </summary>
    public void ClearItems()
    {
        Items.Clear();
        SelectedIndex = -1;
    }

    /// <summary>
    ///     Gets the item at the specified index
    /// </summary>
    /// <param name="index">Index of the item</param>
    /// <returns>The item or null if index is invalid</returns>
    public string? GetItem(int index)
    {
        return index >= 0 && index < Items.Count ? Items[index] : null;
    }

    /// <summary>
    ///     Sets the selected item by value
    /// </summary>
    /// <param name="item">Item to select</param>
    public void SelectItem(string item)
    {
        var index = Items.IndexOf(item);
        if (index >= 0)
        {
            SelectedIndex = index;
        }
    }
}

/// <summary>
///     Event args for selected index changes
/// </summary>
public class SelectedIndexChangedEventArgs : EventArgs
{
    public SelectedIndexChangedEventArgs(int oldIndex, int newIndex)
    {
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }

    public int OldIndex { get; }
    public int NewIndex { get; }
}