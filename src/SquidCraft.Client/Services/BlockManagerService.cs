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
    private readonly Dictionary<BlockType, BlockDefinitionData> _blockDefinitions = new();

    public BlockManagerService()
    {
        _assetManagerService = SquidCraftClientContext.AssetManagerService;
    }


    public void AddBlockDefinition(string atlasName, BlockDefinitionData blockDefinitionData)
    {
        _logger.Information("Adding block definition: {BlockType} faces: {FacesCount}", blockDefinitionData.BlockType, blockDefinitionData.Sides.Count);

        _blockSideEntities[blockDefinitionData.BlockType] = [];
        _blockDefinitions[blockDefinitionData.BlockType] = blockDefinitionData;

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

    /// <summary>
    /// Retrieves the raw block definition metadata for the specified block type.
    /// </summary>
    /// <param name="blockType">Type of block requested.</param>
    /// <returns>The stored block definition, or null when unknown.</returns>
    public BlockDefinitionData? GetBlockDefinition(BlockType blockType)
    {
        if (_blockDefinitions.TryGetValue(blockType, out var definition))
        {
            return definition;
        }

        _logger.Warning("Block definition {BlockType} not found", blockType);
        return null;
    }

    /// <summary>
    /// Determines whether the block type should be treated as transparent when rendering.
    /// </summary>
    /// <param name="blockType">Type of block requested.</param>
    /// <returns>True when the block is transparent or no definition is available.</returns>
    public bool IsTransparent(BlockType blockType)
    {
        if (_blockDefinitions.TryGetValue(blockType, out var definition))
        {
            return definition.IsTransparent;
        }

        // Default to transparent for unknown block types to prevent rendering artifacts.
        return true;
    }
}

public record BlockSideEntity(SideType Side, Texture2DRegion Texture);
