using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Services;
using System.Collections.ObjectModel;

namespace SquidCraft.Client.Components.UI;

/// <summary>
///     TabControl component for managing multiple tab pages
/// </summary>
public class TabControl : GameComponent, IDrawable
{
    private int _selectedIndex = -1;
    private AssetManagerService _assetManagerService;
    private MouseState _previousMouseState;
    private SpriteFontBase? _font;

    /// <summary>
    ///     Initializes a new TabControl
    /// </summary>
    /// <param name="game">Game instance</param>
    /// <param name="assetManagerService">Asset manager service</param>
    public TabControl(Microsoft.Xna.Framework.Game game, AssetManagerService assetManagerService    ) : base(game)
    {
        _assetManagerService = assetManagerService;
        TabPages = new Collection<TabPage>();
        TabHeight = 24;
        ContentPadding = 4;

        // Default styling
        SetDefaultColors();
    }

    public int DrawOrder { get; set; }
    public bool Visible { get; set; } = true;
    public event EventHandler<EventArgs>? DrawOrderChanged;
    public event EventHandler<EventArgs>? VisibleChanged;

    /// <summary>
    ///     Gets the collection of tab pages
    /// </summary>
    public Collection<TabPage> TabPages { get; }

    /// <summary>
    ///     Gets or sets the selected tab index
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= -1 && value < TabPages.Count)
            {
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    ///     Gets the selected tab page
    /// </summary>
    public TabPage? SelectedTab => _selectedIndex >= 0 && _selectedIndex < TabPages.Count ? TabPages[_selectedIndex] : null;

    /// <summary>
    ///     Gets or sets whether the tab control is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    ///     Height of the tab headers
    /// </summary>
    public float TabHeight { get; set; }

    /// <summary>
    ///     Padding around the content area
    /// </summary>
    public float ContentPadding { get; set; }

    /// <summary>
    ///     Position of the component
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    ///     Size of the component
    /// </summary>
    public Vector2 Size { get; set; }

    // Color properties
    public Color BackgroundColor { get; set; }
    public Color TabNormalColor { get; set; }
    public Color TabHoverColor { get; set; }
    public Color TabSelectedColor { get; set; }
    public Color TabDisabledColor { get; set; }
    public Color TabBorderColor { get; set; }
    public Color ContentBackgroundColor { get; set; }
    public Color TextColor { get; set; }
    public Color DisabledTextColor { get; set; }

    private GraphicsDevice GraphicsDevice { get; set; }

    /// <summary>
    ///     Event fired when the selected tab changes
    /// </summary>
    public event EventHandler? SelectedIndexChanged;

    /// <summary>
    ///     Sets default color scheme
    /// </summary>
    private void SetDefaultColors()
    {
        BackgroundColor = new Color(240, 240, 240);
        TabNormalColor = new Color(200, 200, 200);
        TabHoverColor = new Color(220, 220, 220);
        TabSelectedColor = new Color(255, 255, 255);
        TabDisabledColor = new Color(180, 180, 180);
        TabBorderColor = new Color(118, 118, 118);
        ContentBackgroundColor = Color.White;
        TextColor = Color.Black;
        DisabledTextColor = Color.Gray;
    }

    /// <summary>
    ///     Initializes the component
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        GraphicsDevice = this.Game.GraphicsDevice;

        // Load default font
        _font = _assetManagerService.GetFontTtf("DefaultFont", 12);
    }

    /// <summary>
    ///     Updates the component state
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled || TabPages.Count == 0)
        {
            base.Update(gameTime);
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        // Handle tab selection
        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            var tabIndex = GetTabIndexAtPosition(mousePosition);
            if (tabIndex >= 0)
            {
                SelectedIndex = tabIndex;
            }
        }

        _previousMouseState = mouseState;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Gets the tab index at the specified position
    /// </summary>
    private int GetTabIndexAtPosition(Vector2 position)
    {
        if (!new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)TabHeight).Contains(position))
        {
            return -1;
        }

        var tabWidth = Size.X / TabPages.Count;
        var relativeX = position.X - Position.X;
        var tabIndex = (int)(relativeX / tabWidth);

        return tabIndex >= 0 && tabIndex < TabPages.Count ? tabIndex : -1;
    }

    /// <summary>
    ///     Draws the component content
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public void Draw(GameTime gameTime)
    {
        if (_assetManagerService.GetPixelTexture() == null || _font == null || TabPages.Count == 0)
        {
            return;
        }

        var spriteBatch = new SpriteBatch(GraphicsDevice);
        spriteBatch.Begin();

        // Draw background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);

        // Draw tab headers
        DrawTabHeaders(spriteBatch);

        // Draw content area
        DrawContentArea(spriteBatch);

        spriteBatch.End();
    }

    /// <summary>
    ///     Draws the tab headers
    /// </summary>
    private void DrawTabHeaders(SpriteBatch spriteBatch)
    {
        var tabWidth = Size.X / TabPages.Count;

        for (var i = 0; i < TabPages.Count; i++)
        {
            var tab = TabPages[i];
            var tabBounds = new Rectangle(
                (int)(Position.X + i * tabWidth),
                (int)Position.Y,
                (int)tabWidth,
                (int)TabHeight
            );

            // Determine tab color
            Color tabColor;
            if (!IsEnabled || !tab.IsEnabled)
            {
                tabColor = TabDisabledColor;
            }
            else if (i == _selectedIndex)
            {
                tabColor = TabSelectedColor;
            }
            else
            {
                // Check hover
                var mouseState = Mouse.GetState();
                var mousePosition = new Vector2(mouseState.X, mouseState.Y);
                tabColor = tabBounds.Contains(mousePosition) ? TabHoverColor : TabNormalColor;
            }

            // Draw tab background
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), tabBounds, tabColor);

            // Draw tab border
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(tabBounds.X, tabBounds.Y, tabBounds.Width, 1), TabBorderColor);
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(tabBounds.X, tabBounds.Bottom - 1, tabBounds.Width, 1), TabBorderColor);
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(tabBounds.X, tabBounds.Y, 1, tabBounds.Height), TabBorderColor);
            spriteBatch.Draw(_assetManagerService.GetPixelTexture(), new Rectangle(tabBounds.Right - 1, tabBounds.Y, 1, tabBounds.Height), TabBorderColor);

            // Draw tab text
            var textColor = (IsEnabled && tab.IsEnabled) ? TextColor : DisabledTextColor;
            var textSize = _font.MeasureString(tab.Text);
            var textPosition = new Vector2(
                tabBounds.X + (tabBounds.Width - textSize.X) / 2,
                tabBounds.Y + (tabBounds.Height - textSize.Y) / 2
            );

            spriteBatch.DrawString(_font, tab.Text, textPosition, textColor);
        }
    }

    /// <summary>
    ///     Draws the content area
    /// </summary>
    private void DrawContentArea(SpriteBatch spriteBatch)
    {
        var contentBounds = new Rectangle(
            (int)(Position.X + ContentPadding),
            (int)(Position.Y + TabHeight + ContentPadding),
            (int)(Size.X - 2 * ContentPadding),
            (int)(Size.Y - TabHeight - 2 * ContentPadding)
        );

        // Draw content background
        spriteBatch.Draw(_assetManagerService.GetPixelTexture(), contentBounds, ContentBackgroundColor);

        // Draw selected tab content
        if (SelectedTab != null)
        {
            // For simplicity, assume components are drawn by the game loop
            // In a real implementation, you would iterate through SelectedTab.Components
            // and call their Draw methods, but since they are IUIComponent, need to extend
        }
    }

    /// <summary>
    ///     Adds a new tab page
    /// </summary>
    /// <param name="text">Tab text</param>
    /// <param name="tag">Optional tag</param>
    /// <returns>The created TabPage</returns>
    public TabPage AddTab(string text, object? tag = null)
    {
        var tabPage = new TabPage(text, tag);
        TabPages.Add(tabPage);

        if (_selectedIndex == -1)
        {
            _selectedIndex = 0;
        }

        return tabPage;
    }

    /// <summary>
    ///     Removes a tab page
    /// </summary>
    /// <param name="tabPage">Tab page to remove</param>
    public void RemoveTab(TabPage tabPage)
    {
        var index = TabPages.IndexOf(tabPage);
        if (index >= 0)
        {
            TabPages.RemoveAt(index);

            if (_selectedIndex >= TabPages.Count)
            {
                _selectedIndex = TabPages.Count - 1;
            }
        }
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