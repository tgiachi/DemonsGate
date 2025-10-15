using System.Numerics;
using Serilog;
using SquidCraft.Client.Context;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Client.Systems;

public class ChunkLightSystem
{
    private readonly ILogger _logger = Log.ForContext<ChunkLightSystem>();

    private const byte MaxLightLevel = 15;
    private const byte MinLightLevel = 0;
    private const byte SunlightReduction = 1;
    private const byte IndirectLightReduction = 2;

    public void CalculateInitialSunlight(ChunkEntity chunk)
    {
        var lightLevels = new byte[ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height];
        var isProcessed = new bool[ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height];

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                ProcessSunlightColumn(chunk, lightLevels, isProcessed, x, z);
            }
        }

        PropagateHorizontalLight(chunk, lightLevels, isProcessed);

        chunk.SetLightLevels(lightLevels);
        
        _logger.Debug("Calculated sunlight for chunk at {Position}", chunk.Position);
    }

    private void ProcessSunlightColumn(
        ChunkEntity chunk,
        byte[] lightLevels,
        bool[] isProcessed,
        int x,
        int z)
    {
        byte currentLight = MaxLightLevel;
        var hitSolidBlock = false;

        for (int y = ChunkEntity.Height - 1; y >= 0; y--)
        {
            var index = ChunkEntity.GetIndex(x, y, z);
            var block = chunk.Blocks[index];

            if (block == null || block.BlockType == BlockType.Air)
            {
                lightLevels[index] = currentLight;
                isProcessed[index] = true;

                if (hitSolidBlock)
                {
                    currentLight = (byte)Math.Max(MinLightLevel, currentLight - SunlightReduction);
                }
            }
            else
            {
                var blockDef = SquidCraftClientContext.BlockManagerService.GetBlockDefinition(block.BlockType);
                var isTransparent = blockDef?.IsTransparent ?? false;

                if (isTransparent)
                {
                    lightLevels[index] = currentLight;
                    isProcessed[index] = true;
                    currentLight = (byte)Math.Max(MinLightLevel, currentLight - SunlightReduction);
                }
                else
                {
                    lightLevels[index] = currentLight;
                    isProcessed[index] = true;
                    hitSolidBlock = true;
                    currentLight = 0;
                }
            }
        }
    }

    private void PropagateHorizontalLight(
        ChunkEntity chunk,
        byte[] lightLevels,
        bool[] isProcessed)
    {
        bool changes;
        do
        {
            changes = false;

            for (int x = 0; x < ChunkEntity.Size; x++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    for (int z = 0; z < ChunkEntity.Size; z++)
                    {
                        var index = ChunkEntity.GetIndex(x, y, z);

                        if (!isProcessed[index])
                        {
                            continue;
                        }

                        var block = chunk.Blocks[index];
                        if (block != null && block.BlockType != BlockType.Air)
                        {
                            continue;
                        }

                        var highestNeighbor = GetHighestNeighborLight(chunk, lightLevels, x, y, z);

                        if (highestNeighbor > lightLevels[index] + IndirectLightReduction)
                        {
                            lightLevels[index] = (byte)(highestNeighbor - IndirectLightReduction);
                            changes = true;
                        }
                    }
                }
            }
        } while (changes);
    }

    private byte GetHighestNeighborLight(
        ChunkEntity chunk,
        byte[] lightLevels,
        int x,
        int y,
        int z)
    {
        byte highest = 0;

        var neighbors = new[]
        {
            (x + 1, y, z),
            (x - 1, y, z),
            (x, y + 1, z),
            (x, y - 1, z),
            (x, y, z + 1),
            (x, y, z - 1)
        };

        foreach (var (nx, ny, nz) in neighbors)
        {
            if (!chunk.IsInBounds(nx, ny, nz))
            {
                continue;
            }

            var neighborIndex = ChunkEntity.GetIndex(nx, ny, nz);
            highest = Math.Max(highest, lightLevels[neighborIndex]);
        }

        return highest;
    }
}
