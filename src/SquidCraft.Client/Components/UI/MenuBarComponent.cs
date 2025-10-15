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
///     MenuBar component for displaying menu items with dropdown submenus
/// </summary>
public class MenuBarComponent : BaseComponent
{
    private IAssetManagerService _assetManagerService;
    private SpriteFontBase? _font;
    private MouseState _previousMouseState;
    private MenuItemComponent? _hoveredItem;
    private MenuItemComponent? _openMenu;
    private bool _isMenuOpen;

    /// <summary>
    ///     Initializes a new MenuBar component
    /// </summary>
    public MenuBarComponent()
    {
        MenuItems = new ObservableCollection<MenuItemComponent>();
        Size = new Vector2(800, 24); // Default size, will be adjusted based on content

        // Default styling
        SetDefaultColors();
    }

    /// <summary>
    ///     Gets the collection of menu items
    /// </summary>
    public ObservableCollection<MenuItemComponent> MenuItems { get; }

    /// <summary>
    ///     Gets or sets whether the menu bar is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Height of the menu bar
    /// </summary>
    public float MenuHeight { get; set; } = 24f;

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color ItemNormalColor { get; set; }
    public Color ItemHoverColor { get; set; }
    public Color ItemPressedColor { get; set; }
    public Color TextColor { get; set; }
    public Color DisabledTextColor { get; set; }
    public Color SeparatorColor { get; set; }
    public Color SubMenuBackgroundColor { get; set; }
    public Color SubMenuBorderColor { get; set; }

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
        BackgroundColor = new Color(240, 240, 240);
        ItemNormalColor = new Color(240, 240, 240);
        ItemHoverColor = new Color(229, 241, 251);
        ItemPressedColor = new Color(204, 228, 247);
        TextColor = Color.Black;
        DisabledTextColor = Color.Gray;
        SeparatorColor = new Color(200, 200, 200);
        SubMenuBackgroundColor = Color.White;
        SubMenuBorderColor = new Color(118, 118, 118);
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
            _hoveredItem = null;
            CloseMenu();
            base.Update(gameTime);
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        // Update menu items
        _hoveredItem = null;
        var currentX = Position.X;

        foreach (var item in MenuItems)
        {
            if (!item.IsEnabled)
            {
                continue;
            }

            var itemWidth = GetItemWidth(item);
            var itemBounds = new Rectangle((int)currentX, (int)Position.Y, (int)itemWidth, (int)MenuHeight);

            if (itemBounds.Contains(mousePosition))
            {
                _hoveredItem = item;

                // Open submenu on hover if a menu is already open
                if (_isMenuOpen && _openMenu != item)
                {
                    OpenMenu(item);
                }
            }

            currentX += itemWidth;
        }

        // Handle mouse clicks
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            if (_hoveredItem != null)
            {
                if (_isMenuOpen && _openMenu == _hoveredItem)
                {
                    CloseMenu();
                }
                else
                {
                    OpenMenu(_hoveredItem);
                }
            }
            else if (_isMenuOpen)
            {
                CloseMenu();
            }
        }

        // Update open submenu
        if (_openMenu != null)
        {
            UpdateSubMenu(_openMenu, mousePosition, mouseState);
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Updates the currently open submenu
    /// </summary>
    private void UpdateSubMenu(MenuItemComponent menuItem, Vector2 mousePosition, MouseState mouseState)
    {
        if (menuItem.SubItems.Count == 0)
        {
            return;
        }

        var menuBounds = GetSubMenuBounds(menuItem);

        // Check if mouse is still over the menu or parent item
        var parentBounds = GetItemBounds(menuItem);
        var isOverMenu = menuBounds.Contains(mousePosition) || parentBounds.Contains(mousePosition);

        if (!isOverMenu)
        {
            CloseMenu();
            return;
        }

        // Handle submenu item clicks
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            var relativeY = mousePosition.Y - menuBounds.Y;
            var itemIndex = (int)(relativeY / MenuHeight);

            if (itemIndex >= 0 && itemIndex < menuItem.SubItems.Count)
            {
                var subItem = menuItem.SubItems[itemIndex];
                if (subItem.IsEnabled && !subItem.IsSeparator)
                {
                    subItem.PerformClick();
                    CloseMenu();
                }
            }
        }
    }

    /// <summary>
    ///     Opens a submenu
    /// </summary>
    private void OpenMenu(MenuItemComponent menuItem)
    {
        _openMenu = menuItem;
        _isMenuOpen = true;
    }

    /// <summary>
    ///     Closes the currently open menu
    /// </summary>
    private void CloseMenu()
    {
        _openMenu = null;
        _isMenuOpen = false;
    }

    /// <summary>
    ///     Gets the width of a menu item
    /// </summary>
    private float GetItemWidth(MenuItemComponent item)
    {
        if (_font == null)
        {
            return 50f;
        }

        return _font.MeasureString(item.Text).X + 16; // Padding
    }

    /// <summary>
    ///     Gets the bounds of a menu item
    /// </summary>
    private Rectangle GetItemBounds(MenuItemComponent item)
    {
        var currentX = Position.X;
        foreach (var menuItem in MenuItems)
        {
            var itemWidth = GetItemWidth(menuItem);
            if (menuItem == item)
            {
                return new Rectangle((int)currentX, (int)Position.Y, (int)itemWidth, (int)MenuHeight);
            }
            currentX += itemWidth;
        }
        return Rectangle.Empty;
    }

    /// <summary>
    ///     Gets the bounds of a submenu
    /// </summary>
    private Rectangle GetSubMenuBounds(MenuItemComponent menuItem)
    {
        var parentBounds = GetItemBounds(menuItem);
        var maxWidth = 0f;

        foreach (var subItem in menuItem.SubItems)
        {
            if (_font != null)
            {
                maxWidth = Math.Max(maxWidth, _font.MeasureString(subItem.Text).X + 32); // Padding for icons/shortcuts
            }
        }

        return new Rectangle(
            parentBounds.X,
            parentBounds.Bottom,
            (int)maxWidth,
            menuItem.SubItems.Count * (int)MenuHeight
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
        var bounds = new Rectangle((int)position.X, (int)position.Y, (int)Size.X, (int)MenuHeight);

        // Draw background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), bounds, BackgroundColor);

        // Draw menu items
        var currentX = position.X;
        foreach (var item in MenuItems)
        {
            var itemWidth = GetItemWidth(item);
            var itemBounds = new Rectangle((int)currentX, (int)position.Y, (int)itemWidth, (int)MenuHeight);

            var isHovered = item == _hoveredItem;
            var isOpen = item == _openMenu;

            // Draw item background
            var bgColor = isOpen || isHovered ? ItemHoverColor : ItemNormalColor;
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), itemBounds, bgColor);

            // Draw item text
            var textColor = item.IsEnabled ? TextColor : DisabledTextColor;
            var textPosition = new Vector2(
                currentX + 8,
                position.Y + (MenuHeight - _font.LineHeight) / 2
            );
            spriteBatch.DrawString(_font, item.Text, textPosition, textColor);

            currentX += itemWidth;
        }

        // Draw open submenu
        if (_openMenu != null && _isMenuOpen)
        {
            DrawSubMenu(spriteBatch, _openMenu, parentPosition);
        }
    }

    /// <summary>
    ///     Draws a submenu
    /// </summary>
    private void DrawSubMenu(SpriteBatch spriteBatch, MenuItemComponent menuItem, Vector2 parentPosition)
    {
        var menuBounds = GetSubMenuBounds(menuItem);
        menuBounds.X += (int)parentPosition.X;
        menuBounds.Y += (int)parentPosition.Y;

        // Draw submenu background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), menuBounds, SubMenuBackgroundColor);

        // Draw submenu border
        DrawBorder(spriteBatch, menuBounds);

        // Draw submenu items
        var currentY = menuBounds.Y;
        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        for (var i = 0; i < menuItem.SubItems.Count; i++)
        {
            var subItem = menuItem.SubItems[i];
            var itemBounds = new Rectangle(menuBounds.X, currentY, menuBounds.Width, (int)MenuHeight);

            var isHovered = itemBounds.Contains(mousePosition) && subItem.IsEnabled && !subItem.IsSeparator;

            // Draw item background
            if (isHovered)
            {
                spriteBatch.Draw(_assetManagerService.GetPixelTexture(), itemBounds, ItemHoverColor);
            }

            if (subItem.IsSeparator)
            {
                // Draw separator line
                var separatorY = currentY + MenuHeight / 2;
                spriteBatch.Draw(_assetManagerService.GetPixelTexture(),
                    new Rectangle(menuBounds.X + 4, (int)separatorY, menuBounds.Width - 8, 1), SeparatorColor);
            }
            else
            {
                // Draw item text
                var textColor = subItem.IsEnabled ? TextColor : DisabledTextColor;
                var textPosition = new Vector2(
                    menuBounds.X + 8,
                    currentY + (MenuHeight - _font.LineHeight) / 2
                );
                spriteBatch.DrawString(_font, subItem.Text, textPosition, textColor);

                // Draw shortcut text if available
                if (!string.IsNullOrEmpty(subItem.ShortcutText))
                {
                    var shortcutSize = _font.MeasureString(subItem.ShortcutText);
                    var shortcutPosition = new Vector2(
                        menuBounds.Right - shortcutSize.X - 8,
                        currentY + (MenuHeight - _font.LineHeight) / 2
                    );
                    spriteBatch.DrawString(_font, subItem.ShortcutText, shortcutPosition, textColor);
                }
            }

            currentY += (int)MenuHeight;
        }
    }

    /// <summary>
    ///     Draws a border around a rectangle
    /// </summary>
    private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var pixel = _assetManagerService.GetPixelTexture();
        if (pixel == null)
        {
            return;
        }

        // Top
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), SubMenuBorderColor);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), SubMenuBorderColor);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), SubMenuBorderColor);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), SubMenuBorderColor);
    }

    /// <summary>
    ///     Adds a new menu item
    /// </summary>
    /// <param name="text">Menu item text</param>
    /// <returns>The created MenuItemComponent</returns>
    public MenuItemComponent AddMenuItem(string text)
    {
        var item = new MenuItemComponent(text);
        MenuItems.Add(item);
        return item;
    }
}

/// <summary>
///     Represents a menu item in the menu bar
/// </summary>
public class MenuItemComponent
{
    /// <summary>
    ///     Initializes a new MenuItem
    /// </summary>
    /// <param name="text">The text to display</param>
    public MenuItemComponent(string text)
    {
        Text = text;
        SubItems = new ObservableCollection<MenuItemComponent>();
    }

    /// <summary>
    ///     Gets or sets the text
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    ///     Gets or sets whether this item is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether this is a separator
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    ///     Gets or sets the shortcut text (e.g., "Ctrl+O")
    /// </summary>
    public string? ShortcutText { get; set; }

    /// <summary>
    ///     Gets the sub-items for this menu item
    /// </summary>
    public ObservableCollection<MenuItemComponent> SubItems { get; }

    /// <summary>
    ///     Event fired when this menu item is clicked
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    ///     Performs a click action on this menu item
    /// </summary>
    public void PerformClick()
    {
        Click?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Adds a sub-item to this menu item
    /// </summary>
    /// <param name="text">Sub-item text</param>
    /// <returns>The created MenuItemComponent</returns>
    public MenuItemComponent AddSubItem(string text)
    {
        var item = new MenuItemComponent(text);
        SubItems.Add(item);
        return item;
    }

    /// <summary>
    ///     Adds a separator to the sub-items
    /// </summary>
    public void AddSeparator()
    {
        SubItems.Add(new MenuItemComponent("") { IsSeparator = true });
    }
}