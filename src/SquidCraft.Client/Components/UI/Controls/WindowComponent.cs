using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Components.Interfaces;
using SquidCraft.Client.Components.UI.Layout;
using SquidCraft.Client.Context;
using SquidCraft.Client.Types.Layout;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Draggable window container with a title bar and optional close button.
/// </summary>
public class WindowComponent : BaseComponent
{
    private readonly StackPanelComponent _contentPanel;
    private readonly LabelComponent _titleLabel;
    private readonly ButtonComponent _closeButton;
    private bool _isInitialized;

    private bool _isDragging;
    private Vector2 _dragOffset;

    private Color _titleColor = Color.White;

    public WindowComponent(string title = "Window")
    {
        TitleBarHeight = 28f;
        BackgroundColor = new Color(28, 30, 34);
        BorderColor = new Color(62, 65, 70);
        TitleBarColor = new Color(46, 49, 54);
        AllowDrag = true;
        IsClosable = true;
        MinimumSize = new Vector2(220, 140);

        Position = new Vector2(180, 120);
        _titleLabel = new LabelComponent(title, fontSize: 16)
        {
            IsEnabled = false
        };
        _titleLabel.Color = _titleColor;

        _closeButton = new ButtonComponent("×")
        {
            Size = new Vector2(28, 24)
        };
        _closeButton.Clicked += (_, _) =>
        {
            if (IsClosable)
            {
                IsVisible = false;
            }
        };

        _contentPanel = new StackPanelComponent
        {
            Orientation = StackOrientation.Vertical,
            Alignment = Alignment.Start,
            Padding = new Vector2(12f, 12f),
            Spacing = 8f,
            AutoSize = false,
            HasFocus = true
        };

        AddChild(_contentPanel);
        AddChild(_titleLabel);
        AddChild(_closeButton);

        base.Size = new Vector2(360, 240);
        _isInitialized = true;
        UpdateLayout();
    }

    public Vector2 MinimumSize { get; set; }

    public Color BackgroundColor { get; set; }

    public Color BorderColor { get; set; }

    public Color TitleBarColor { get; set; }

    public Color TitleColor
    {
        get => _titleLabel?.Color ?? _titleColor;
        set
        {
            _titleColor = value;
            if (_titleLabel != null)
            {
                _titleLabel.Color = value;
            }
        }
    }

    public float TitleBarHeight { get; set; }

    public bool AllowDrag { get; set; }

    public bool IsClosable { get; set; }

    public string Title
    {
        get => _titleLabel.Text;
        set => _titleLabel.Text = value ?? string.Empty;
    }

    public void AddContent(ISCDrawableComponent child)
    {
        _contentPanel.AddChild(child);
    }

    public void RemoveContent(ISCDrawableComponent child)
    {
        _contentPanel.RemoveChild(child);
    }

    public void ClearContent()
    {
        _contentPanel.ClearChildren();
    }

    public StackPanelComponent ContentPanel => _contentPanel;

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();
        if (_isInitialized)
        {
            UpdateLayout();
        }
    }

    public override void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled)
        {
            base.HandleMouse(mouseState, gameTime);
            return;
        }

        var mousePosition = new Vector2(mouseState.X, mouseState.Y);
        var titleRect = GetTitleBarRect();

        if (AllowDrag && mouseState.LeftButton == ButtonState.Pressed)
        {
            if (!_isDragging && titleRect.Contains(mousePosition.ToPoint()))
            {
                _isDragging = true;
                _dragOffset = mousePosition - Position;
            }
        }
        else
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            Position = mousePosition - _dragOffset;
            UpdateLayout();
        }

        base.HandleMouse(mouseState, gameTime);
    }

    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        var pixel = SquidCraftClientContext.AssetManagerService.GetPixelTexture();
        var absolute = Position + parentPosition;
        var resolved = ResolveSize();

        var windowRect = new Rectangle((int)absolute.X, (int)absolute.Y, (int)resolved.X, (int)resolved.Y);
        spriteBatch.Draw(pixel, windowRect, BackgroundColor * Opacity);

        var titleRect = GetTitleBarRect();
        spriteBatch.Draw(pixel, titleRect, TitleBarColor * Opacity);

        DrawBorder(spriteBatch, pixel, windowRect);
    }

    private void UpdateLayout()
    {
        if (!_isInitialized)
        {
            return;
        }

        var clampedWidth = Math.Max(MinimumSize.X, Size.X);
        var clampedHeight = Math.Max(MinimumSize.Y, Size.Y);
        base.Size = new Vector2(clampedWidth, clampedHeight);

        var absolute = Position;

        _titleLabel.Position = absolute + new Vector2(12f, (TitleBarHeight - _titleLabel.Size.Y) / 2f);
        _closeButton.Position = absolute + new Vector2(Size.X - _closeButton.Size.X - 10f, (TitleBarHeight - _closeButton.Size.Y) / 2f);

        _contentPanel.Position = absolute + new Vector2(0f, TitleBarHeight);
        _contentPanel.Size = new Vector2(Size.X, Size.Y - TitleBarHeight);
        _contentPanel.RequestLayout();
    }

    private Rectangle GetTitleBarRect()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)TitleBarHeight);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), BorderColor * Opacity);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), BorderColor * Opacity);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), BorderColor * Opacity);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), BorderColor * Opacity);
    }
}
