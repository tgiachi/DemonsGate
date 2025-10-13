namespace SquidCraft.Client.Types;

/// <summary>
/// Defines the types of assets that can be loaded and managed by the asset manager.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Unknown or unsupported asset type
    /// </summary>
    Unknown,

    /// <summary>
    /// TrueType Font (.ttf, .otf)
    /// </summary>
    FontTtf,

    /// <summary>
    /// Bitmap Font (.fnt)
    /// </summary>
    FontBitmap,

    /// <summary>
    /// TheDraw Font (.tdf)
    /// </summary>
    TheDrawFont,

    /// <summary>
    /// Image/Texture (.png, .jpg, .jpeg, .bmp, .gif)
    /// </summary>
    Image,

    /// <summary>
    /// Texture Atlas (.atl)
    /// </summary>
    Atlas,

    /// <summary>
    /// Sound Effect (.wav, .mp3, .ogg)
    /// </summary>
    Sound,

    /// <summary>
    /// Music Track (.mp3, .ogg, .wma, .m4a)
    /// </summary>
    Music
}
