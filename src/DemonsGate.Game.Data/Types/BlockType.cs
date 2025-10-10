namespace DemonsGate.Services.Game.Types;

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

}
