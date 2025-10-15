using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

public class TabControlComponent : BaseComponent
{
    private int _selectedIndex = -1;
    private MouseState _previousMouseState;
    private SpriteFontBase? _font;
    private readonly List<TabPageComponent> _tabPages = new();
    private int _hoveredTabIndex = -1;

    public float TabHeight { get; set; } = 24f;
    public float ContentPadding { get; set; } = 4f;

    public Color BackgroundColor { get; set; } = new Color(240, 240, 240);
    public Color TabNormalColor { get; set; } = new Color(200, 200, 200);
    public Color TabHoverColor { get; set; } = new Color(220, 220, 220);
    public Color TabSelectedColor { get; set; } = new Color(255, 255, 255);
    public Color TabDisabledColor { get; set; } = new Color(180, 180, 180);
    public Color TabBorderColor { get; set; } = new Color(118, 118, 118);
    public Color ContentBackgroundColor { get; set; } = Color.White;
    public Color TextColor { get; set; } = Color.Black;
    public Color DisabledTextColor { get; set; } = Color.Gray;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value >= -1 && value < _tabPages.Count && _selectedIndex != value)
            {
                _selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public TabPageComponent? SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabPages.Count 
        ? _tabPages[_selectedIndex] 
        : null;

    public IReadOnlyList<TabPageComponent> TabPages => _tabPages;

    public event EventHandler? SelectedIndexChanged;

    public override void Initialize()
    {
        base.Initialize();
        _font = SquidCraftClientContext.AssetManagerService.GetFontTtf("DefaultFont", 12);
    }

    public TabPageComponent AddTab(string text)
    {
        var tabPage = new TabPageComponent(text);
        _tabPages.Add(tabPage);

        if (_selectedIndex == -1)
        {
            _selectedIndex = 0;
        }

        return tabPage;
    }

    public void RemoveTab(TabPageComponent tabPage)
    {
        var index = _tabPages.IndexOf(tabPage);
        if (index >= 0)
        {
            _tabPages.RemoveAt(index);

            if (_selectedIndex >= _tabPages.Count)
            {
                _selectedIndex = _tabPages.Count - 1;
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsEnabled || _tabPages.Count == 0)
        {
            return;
        }

        var mouseState = Mouse.GetState();
        var mousePosition = new Vector2(mouseState.X, mouseState.Y);

        _hoveredTabIndex = GetTabIndexAtPosition(mousePosition);

        if (mouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released &&
            _hoveredTabIndex >= 0)
        {
            SelectedIndex = _hoveredTabIndex;
        }

        _previousMouseState = mouseState;
    }

    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (SquidCraftClientContext.AssetManagerService.GetPixelTexture() == null || 
            _font == null || 
            _tabPages.Count == 0)
        {
            return;
        }

        var position = Position + parentPosition;
        var pixelTexture = SquidCraftClientContext.AssetManagerService.GetPixelTexture();

        spriteBatch.Draw(pixelTexture, new Rectangle((int)position.X, (int)position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);

        DrawTabHeaders(spriteBatch, position, pixelTexture);
        DrawContentArea(spriteBatch, position, pixelTexture);
    }

    private void DrawTabHeaders(SpriteBatch spriteBatch, Vector2 position, Texture2D pixelTexture)
    {
        var tabWidth = Size.X / _tabPages.Count;

        for (var i = 0; i < _tabPages.Count; i++)
        {
            var tab = _tabPages[i];
            var tabBounds = new Rectangle(
                (int)(position.X + i * tabWidth),
                (int)position.Y,
                (int)tabWidth,
                (int)TabHeight
            );

            Color tabColor;
            if (!IsEnabled || !tab.IsEnabled)
            {
                tabColor = TabDisabledColor;
            }
            else if (i == _selectedIndex)
            {
                tabColor = TabSelectedColor;
            }
            else if (i == _hoveredTabIndex)
            {
                tabColor = TabHoverColor;
            }
            else
            {
                tabColor = TabNormalColor;
            }

            spriteBatch.Draw(pixelTexture, tabBounds, tabColor);

            spriteBatch.Draw(pixelTexture, new Rectangle(tabBounds.X, tabBounds.Y, tabBounds.Width, 1), TabBorderColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(tabBounds.X, tabBounds.Bottom - 1, tabBounds.Width, 1), TabBorderColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(tabBounds.X, tabBounds.Y, 1, tabBounds.Height), TabBorderColor);
            spriteBatch.Draw(pixelTexture, new Rectangle(tabBounds.Right - 1, tabBounds.Y, 1, tabBounds.Height), TabBorderColor);

            var textColor = (IsEnabled && tab.IsEnabled) ? TextColor : DisabledTextColor;
            var textSize = _font.MeasureString(tab.Text);
            var textPosition = new Vector2(
                tabBounds.X + (tabBounds.Width - textSize.X) / 2,
                tabBounds.Y + (tabBounds.Height - textSize.Y) / 2
            );

            spriteBatch.DrawString(_font, tab.Text, textPosition, textColor);
        }
    }

    private void DrawContentArea(SpriteBatch spriteBatch, Vector2 position, Texture2D pixelTexture)
    {
        var contentBounds = new Rectangle(
            (int)(position.X + ContentPadding),
            (int)(position.Y + TabHeight + ContentPadding),
            (int)(Size.X - 2 * ContentPadding),
            (int)(Size.Y - TabHeight - 2 * ContentPadding)
        );

        spriteBatch.Draw(pixelTexture, contentBounds, ContentBackgroundColor);

        if (SelectedTab != null && _font != null)
        {
            var textPosition = new Vector2(contentBounds.X + 10, contentBounds.Y + 10);
            spriteBatch.DrawString(_font, SelectedTab.Content, textPosition, TextColor);
        }
    }

    private int GetTabIndexAtPosition(Vector2 mousePosition)
    {
        if (!new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)TabHeight).Contains(mousePosition))
        {
            return -1;
        }

        var tabWidth = Size.X / _tabPages.Count;
        var relativeX = mousePosition.X - Position.X;
        var tabIndex = (int)(relativeX / tabWidth);

        return tabIndex >= 0 && tabIndex < _tabPages.Count ? tabIndex : -1;
    }
}

public class TabPageComponent
{
    public TabPageComponent(string text)
    {
        Text = text;
        Content = string.Empty;
    }

    public string Text { get; set; }
    public string Content { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public object? Tag { get; set; }
}
