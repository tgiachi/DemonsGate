using SquidCraft.Game.Data.Types;

namespace SquidCraft.Game.Data.Assets;

public class BlockDefinitionData
{
    public BlockType BlockType { get; set; }
    public Dictionary<SideType, string> Sides { get; set; } = new();

    public void AddSide(SideType type, string side)
    {
        Sides.Add(type, side);
    }
}
