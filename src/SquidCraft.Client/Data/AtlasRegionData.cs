namespace SquidCraft.Client.Data;

/// <summary>
/// Data format for individual atlas regions
/// </summary>
internal sealed class AtlasRegionData
{
    public string Name { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
