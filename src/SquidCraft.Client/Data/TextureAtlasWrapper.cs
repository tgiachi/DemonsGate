using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;

namespace SquidCraft.Client.Data;

/// <summary>
/// Wrapper for texture atlas that supports variable-sized regions
/// </summary>
internal sealed class TextureAtlasWrapper
{
    private readonly string _name;
    private readonly Texture2D _texture;
    private readonly Dictionary<string, Texture2DRegion> _regions;

    public Texture2DAtlas Atlas { get; }

    public TextureAtlasWrapper(string name, Texture2D texture, Dictionary<string, Texture2DRegion> regions)
    {
        _name = name;
        _texture = texture;
        _regions = regions;

        // Create Texture2DAtlas using the Create method with uniform grid (1x1 to use the entire texture)
        // Then we'll override region access through our wrapper
        Atlas = Texture2DAtlas.Create(_name, _texture, _texture.Width, _texture.Height);
    }

    public Texture2DRegion? GetRegion(string regionName)
    {
        return _regions.GetValueOrDefault(regionName);
    }

    public IEnumerable<string> GetRegionNames() => _regions.Keys;

    public int RegionCount => _regions.Count;
}
