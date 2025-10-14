using SquidCraft.Game.Data.Primitives;

namespace SquidCraft.Client.Services;

public class ServerChunkProvider
{
    public async Task<ChunkEntity> RequestChunkFromServerAsync(int chunkX, int chunkZ)
    {
        await Task.Delay(50);

        var chunkOrigin = new System.Numerics.Vector3(
            chunkX * ChunkEntity.Size,
            0f,
            chunkZ * ChunkEntity.Size
        );

        var chunk = new ChunkEntity(chunkOrigin);
        long id = (chunkX * 1000000L) + (chunkZ * 1000L) + 1;

        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                var isWater = (x > 5 && x < 10 && z > 5 && z < 10);

                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    Game.Data.Types.BlockType blockType;

                    if (y == 0)
                    {
                        blockType = Game.Data.Types.BlockType.Bedrock;
                    }
                    else if (y < ChunkEntity.Height - 2)
                    {
                        blockType = Game.Data.Types.BlockType.Dirt;
                    }
                    else if (y < ChunkEntity.Height - 1)
                    {
                        blockType = isWater ? Game.Data.Types.BlockType.Dirt : Game.Data.Types.BlockType.Dirt;
                    }
                    else
                    {
                        blockType = isWater ? Game.Data.Types.BlockType.Water : Game.Data.Types.BlockType.Grass;
                    }

                    chunk.SetBlock(x, y, z, new BlockEntity(id++, blockType));
                }
            }
        }

        return chunk;
    }
}
