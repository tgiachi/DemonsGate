namespace DemonsGate.Game.Data.Types;

/// <summary>
/// Enumerates the available block types that can populate world chunks.
/// </summary>
public enum BlockType : byte
{
    /// <summary>
    /// Empty space with no solid block.
    /// </summary>
    Air,
    /// <summary>
    /// Standard soil block.
    /// </summary>
    Dirt,
    /// <summary>
    /// Grass-covered surface block.
    /// </summary>
    Grass,

    /// <summary>
    ///  The end of map
    /// </summary>
    Bedrock,

    /// <summary>
    /// Snow block for cold biomes.
    /// </summary>
    Snow,

    /// <summary>
    /// Ice block, frozen water.
    /// </summary>
    Ice,

    /// <summary>
    /// Moss block for decoration.
    /// </summary>
    Moss,

}
