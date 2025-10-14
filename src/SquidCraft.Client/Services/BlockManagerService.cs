using MonoGame.Extended.Graphics;
using Serilog;
using SquidCraft.Client.Context;
using SquidCraft.Client.Interfaces;
using SquidCraft.Game.Data.Assets;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Client.Services;

public class BlockManagerService
{
    private readonly IAssetManagerService  _assetManagerService;

    private readonly ILogger _logger = Log.ForContext<BlockManagerService>();

    private readonly Dictionary<BlockType, List<BlockSideEntity>> _blockSideEntities = new();

    public BlockManagerService()
    {
        _assetManagerService = SquidCraftClientContext.AssetManagerService;
    }


    public void AddBlockDefinition(string atlasName, BlockDefinitionData blockDefinitionData)
    {
        _logger.Information("Adding block definition: {BlockType} faces: {FacesCount}", blockDefinitionData.BlockType, blockDefinitionData.Sides.Count);

        _blockSideEntities[blockDefinitionData.BlockType] = [];

        var atlas = _assetManagerService.GetAtlas(atlasName);

        foreach (var side in blockDefinitionData.Sides)
        {
            var index = int.Parse(side.Value, System.Globalization.CultureInfo.InvariantCulture);
            _blockSideEntities[blockDefinitionData.BlockType].Add(new BlockSideEntity(side.Key, atlas[index]));
        }
    }

    public Texture2DRegion? GetBlockSide(BlockType blockType, SideType sideType)
    {

        if (_blockSideEntities.TryGetValue(blockType, out var sides))
        {
            var side = sides.Find(s => s.Side == sideType);
            return side?.Texture;
        }

        _logger.Warning("Block type {BlockType}  not found", blockType);
        return null;
    }
}

public record BlockSideEntity(SideType Side, Texture2DRegion Texture);
