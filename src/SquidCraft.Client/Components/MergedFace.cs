using SquidCraft.Game.Data.Types;

namespace SquidCraft.Client.Components;

/// <summary>
/// Represents a merged face for greedy meshing.
/// </summary>
internal struct MergedFace
{
    /// <summary>
    /// Gets or sets the block type.
    /// </summary>
    public BlockType BlockType;

    /// <summary>
    /// Gets or sets the X position.
    /// </summary>
    public int X;

    /// <summary>
    /// Gets or sets the Y position.
    /// </summary>
    public int Y;

    /// <summary>
    /// Gets or sets the Z position.
    /// </summary>
    public int Z;

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public int Width;

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public int Height;

    /// <summary>
    /// Gets or sets the side.
    /// </summary>
    public SideType Side;
}