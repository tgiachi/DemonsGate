using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SquidCraft.Client.Components.Base;
using SquidCraft.Client.Context;

namespace SquidCraft.Client.Components.UI.Controls;

/// <summary>
/// Simple component that renders a texture as a background image.
/// </summary>
public class BackgroundImageComponent : BaseComponent
{
    private Texture2D? _texture;
    private string? _textureName;
    private bool _stretchToFit = true;
    private Rectangle? _sourceRectangle;

    /// <summary>
    /// Initializes a new instance of <see cref="BackgroundImageComponent"/>.
    /// </summary>
    /// <param name="textureName">Optional texture name to load via the asset manager.</param>
    public BackgroundImageComponent(string? textureName = null)
    {
        Tint = Color.White;
        _stretchToFit = true;

        if (!string.IsNullOrWhiteSpace(textureName))
        {
            LoadTexture(textureName);
        }
    }

    /// <summary>
    /// Gets or sets the tint color applied when drawing.
    /// </summary>
    public Color Tint { get; set; }

    /// <summary>
    /// Gets or sets the source rectangle to draw. When null the full texture is used.
    /// </summary>
    public Rectangle? SourceRectangle
    {
        get => _sourceRectangle;
        set
        {
            _sourceRectangle = value;
            EnsureSizeFromTexture();
        }
    }

    /// <summary>
    /// Gets or sets whether the texture should stretch to fill the component bounds.
    /// </summary>
    public bool StretchToFit
    {
        get => _stretchToFit;
        set
        {
            if (_stretchToFit != value)
            {
                _stretchToFit = value;
                EnsureSizeFromTexture();
            }
        }
    }

    /// <summary>
    /// Gets the currently assigned texture name, if any.
    /// </summary>
    public string? TextureName => _textureName;

    /// <summary>
    /// Loads a texture by name through the asset manager.
    /// </summary>
    /// <param name="textureName">Name of the texture previously loaded into the asset manager.</param>
    public void LoadTexture(string textureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textureName);

        var assetManager = SquidCraftClientContext.AssetManagerService;
        _texture = assetManager.GetTexture(textureName);
        _textureName = textureName;

        EnsureSizeFromTexture();
    }

    /// <summary>
    /// Assigns an existing texture instance to the component.
    /// </summary>
    /// <param name="texture">Texture to use when drawing.</param>
    public void SetTexture(Texture2D texture)
    {
        _texture = texture ?? throw new ArgumentNullException(nameof(texture));
        _textureName = null;
        EnsureSizeFromTexture();
    }

    /// <inheritdoc />
    protected override void DrawContent(SpriteBatch spriteBatch, GameTime gameTime, Vector2 parentPosition)
    {
        if (_texture == null)
        {
            return;
        }

        var sourceRect = _sourceRectangle ?? _texture.Bounds;
        var absolutePosition = Position + parentPosition;
        var resolvedSize = ResolveSize();

        Rectangle destinationRect;

        if (StretchToFit)
        {
            destinationRect = new Rectangle(
                (int)absolutePosition.X,
                (int)absolutePosition.Y,
                (int)resolvedSize.X,
                (int)resolvedSize.Y);
        }
        else
        {
            destinationRect = new Rectangle(
                (int)absolutePosition.X,
                (int)absolutePosition.Y,
                sourceRect.Width,
                sourceRect.Height);
        }

        spriteBatch.Draw(_texture, destinationRect, sourceRect, Tint * Opacity);
    }

    private void EnsureSizeFromTexture()
    {
        if (StretchToFit || _texture == null)
        {
            return;
        }

        if (Size.LengthSquared() > 0)
        {
            return;
        }

        var source = _sourceRectangle ?? _texture.Bounds;
        base.Size = new Vector2(source.Width, source.Height);
    }
}
