using DemonsGate.Game.Data.Types;

namespace DemonsGate.Game.Data.Assets;

public class BlockDefinitionData
{
    public BlockType BlockType { get; set; }
    public Dictionary<BlockSideType, string> Sides { get; set; } = new();

    public void AddSide(BlockSideType type, string side)
    {
        Sides.Add(type, side);
    }
}
