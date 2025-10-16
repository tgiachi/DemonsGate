using SquidCraft.Game.Data.Types;

namespace SquidCraft.Game.Data.Assets;

public class BlockDefinitionData
{
    public BlockType BlockType { get; set; }
    public Dictionary<SideType, string> Sides { get; set; } = new();

    public bool IsTransparent { get; set; }

    public bool IsLiquid { get; set; }

    public bool IsSolid { get; set; }

    public bool IsBillboard { get; set; }

    public bool IsWindable { get; set; }

    public float WindSpeed { get; set; } = 1.0f;

    public float Height { get; set; } = 1.0f;

    public void AddSide(SideType type, string side)
    {
        Sides.Add(type, side);
    }

    public override string ToString()
    {
        return "BlockDefinitionData { BlockType: " + BlockType + ", Sides: " + Sides.Count + " }";
    }
}
