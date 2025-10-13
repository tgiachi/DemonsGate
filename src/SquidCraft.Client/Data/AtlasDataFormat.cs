namespace SquidCraft.Client.Data;

/// <summary>
/// Data format for atlas JSON files
/// </summary>
internal sealed class AtlasDataFormat
{
    public List<AtlasRegionData> Regions { get; set; } = new();
}
