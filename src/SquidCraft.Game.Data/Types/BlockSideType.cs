namespace SquidCraft.Game.Data.Types;

/// <summary>
/// Enumerates the six faces of a block in 3D space.
/// </summary>
public enum BlockSideType : byte
{
    /// <summary>
    /// Top face of the block (Y+).
    /// </summary>
    Top,

    /// <summary>
    /// Bottom face of the block (Y-).
    /// </summary>
    Bottom,

    /// <summary>
    /// North face of the block (Z-).
    /// </summary>
    North,

    /// <summary>
    /// South face of the block (Z+).
    /// </summary>
    South,

    /// <summary>
    /// East face of the block (X+).
    /// </summary>
    East,

    /// <summary>
    /// West face of the block (X-).
    /// </summary>
    West
}
