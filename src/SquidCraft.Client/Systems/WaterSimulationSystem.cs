using Microsoft.Xna.Framework;
using SquidCraft.Game.Data.Primitives;
using SquidCraft.Game.Data.Types;

namespace SquidCraft.Client.Systems;

public class WaterSimulationSystem
{
    private readonly Queue<Vector3> _waterUpdateQueue = new();
    private readonly HashSet<Vector3> _queuedPositions = new();
    
    public int MaxUpdatesPerFrame { get; set; } = 10;
    
    public bool EnableWaterFlow { get; set; } = true;

    public void QueueWaterUpdate(ChunkEntity chunk, int x, int y, int z)
    {
        var worldPos = new Vector3(
            chunk.Position.X + x,
            chunk.Position.Y + y,
            chunk.Position.Z + z
        );
        
        if (_queuedPositions.Add(worldPos))
        {
            _waterUpdateQueue.Enqueue(worldPos);
        }
    }

    public void Update(Func<Vector3, ChunkEntity?> getChunkAtPosition, 
                      Func<ChunkEntity, int, int, int, BlockEntity?> getBlock,
                      Action<ChunkEntity, int, int, int, BlockEntity> setBlock)
    {
        if (!EnableWaterFlow) return;

        var updatesProcessed = 0;
        
        while (_waterUpdateQueue.Count > 0 && updatesProcessed < MaxUpdatesPerFrame)
        {
            var worldPos = _waterUpdateQueue.Dequeue();
            _queuedPositions.Remove(worldPos);
            
            var chunk = getChunkAtPosition(worldPos);
            if (chunk == null) continue;

            var localX = (int)(worldPos.X - chunk.Position.X);
            var localY = (int)(worldPos.Y - chunk.Position.Y);
            var localZ = (int)(worldPos.Z - chunk.Position.Z);

            if (!chunk.IsInBounds(localX, localY, localZ)) continue;

            var block = getBlock(chunk, localX, localY, localZ);
            if (block == null) continue;

            if (block.BlockType == BlockType.Water && block.WaterLevel > 0)
            {
                UpdateWaterBlock(chunk, localX, localY, localZ, block, getChunkAtPosition, getBlock, setBlock);
            }
            
            updatesProcessed++;
        }
    }

    private void UpdateWaterBlock(
        ChunkEntity chunk, 
        int x, int y, int z, 
        BlockEntity waterBlock,
        Func<Vector3, ChunkEntity?> getChunkAtPosition,
        Func<ChunkEntity, int, int, int, BlockEntity?> getBlock,
        Action<ChunkEntity, int, int, int, BlockEntity> setBlock)
    {
        var waterLevel = waterBlock.WaterLevel;
        
        if (TryFlowDown(chunk, x, y, z, waterLevel, getChunkAtPosition, getBlock, setBlock, waterBlock))
        {
            return;
        }

        if (waterLevel <= 1)
        {
            return;
        }

        var spreadLevel = (byte)(waterLevel - 1);
        
        TryFlowHorizontal(chunk, x, y, z, spreadLevel, getChunkAtPosition, getBlock, setBlock);
    }

    private bool TryFlowDown(
        ChunkEntity chunk,
        int x, int y, int z,
        byte sourceLevel,
        Func<Vector3, ChunkEntity?> getChunkAtPosition,
        Func<ChunkEntity, int, int, int, BlockEntity?> getBlock,
        Action<ChunkEntity, int, int, int, BlockEntity> setBlock,
        BlockEntity sourceBlock)
    {
        var belowY = y - 1;
        if (belowY < 0) return false;

        var belowBlock = GetBlockAt(chunk, x, belowY, z, getChunkAtPosition, getBlock);
        if (belowBlock == null) return false;

        if (belowBlock.BlockType == BlockType.Air)
        {
            var newWaterBlock = new BlockEntity(GenerateId(), BlockType.Water)
            {
                WaterLevel = 7
            };
            
            SetBlockAt(chunk, x, belowY, z, newWaterBlock, getChunkAtPosition, setBlock);
            QueueNeighbors(chunk, x, belowY, z);
            
            if (sourceLevel < 7)
            {
                sourceBlock.WaterLevel = (byte)Math.Max(0, sourceLevel - 1);
                if (sourceBlock.WaterLevel == 0)
                {
                    setBlock(chunk, x, y, z, new BlockEntity(GenerateId(), BlockType.Air));
                }
            }
            
            return true;
        }
        else if (belowBlock.BlockType == BlockType.Water && belowBlock.WaterLevel < 7)
        {
            belowBlock.WaterLevel = 7;
            SetBlockAt(chunk, x, belowY, z, belowBlock, getChunkAtPosition, setBlock);
            QueueNeighbors(chunk, x, belowY, z);
            
            if (sourceLevel < 7)
            {
                sourceBlock.WaterLevel = (byte)Math.Max(0, sourceLevel - 1);
                if (sourceBlock.WaterLevel == 0)
                {
                    setBlock(chunk, x, y, z, new BlockEntity(GenerateId(), BlockType.Air));
                }
            }
            
            return true;
        }

        return false;
    }

    private void TryFlowHorizontal(
        ChunkEntity chunk,
        int x, int y, int z,
        byte spreadLevel,
        Func<Vector3, ChunkEntity?> getChunkAtPosition,
        Func<ChunkEntity, int, int, int, BlockEntity?> getBlock,
        Action<ChunkEntity, int, int, int, BlockEntity> setBlock)
    {
        var directions = new[] 
        { 
            (1, 0),   // East
            (-1, 0),  // West
            (0, 1),   // North
            (0, -1)   // South
        };

        foreach (var (dx, dz) in directions)
        {
            var targetX = x + dx;
            var targetZ = z + dz;

            var targetBlock = GetBlockAt(chunk, targetX, y, targetZ, getChunkAtPosition, getBlock);
            if (targetBlock == null) continue;

            if (targetBlock.BlockType == BlockType.Air)
            {
                var newWaterBlock = new BlockEntity(GenerateId(), BlockType.Water)
                {
                    WaterLevel = spreadLevel
                };
                
                SetBlockAt(chunk, targetX, y, targetZ, newWaterBlock, getChunkAtPosition, setBlock);
                QueueNeighbors(chunk, targetX, y, targetZ);
            }
            else if (targetBlock.BlockType == BlockType.Water && targetBlock.WaterLevel < spreadLevel - 1)
            {
                targetBlock.WaterLevel = (byte)(spreadLevel - 1);
                SetBlockAt(chunk, targetX, y, targetZ, targetBlock, getChunkAtPosition, setBlock);
                QueueNeighbors(chunk, targetX, y, targetZ);
            }
        }
    }

    private BlockEntity? GetBlockAt(
        ChunkEntity originChunk,
        int x, int y, int z,
        Func<Vector3, ChunkEntity?> getChunkAtPosition,
        Func<ChunkEntity, int, int, int, BlockEntity?> getBlock)
    {
        if (y < 0 || y >= ChunkEntity.Height) return null;

        if (originChunk.IsInBounds(x, y, z))
        {
            return getBlock(originChunk, x, y, z);
        }

        var worldX = originChunk.Position.X + x;
        var worldZ = originChunk.Position.Z + z;
        
        var targetChunk = getChunkAtPosition(new Vector3(worldX, y, worldZ));
        if (targetChunk == null) return null;

        var localX = (int)(worldX - targetChunk.Position.X);
        var localZ = (int)(worldZ - targetChunk.Position.Z);

        return getBlock(targetChunk, localX, y, localZ);
    }

    private void SetBlockAt(
        ChunkEntity originChunk,
        int x, int y, int z,
        BlockEntity block,
        Func<Vector3, ChunkEntity?> getChunkAtPosition,
        Action<ChunkEntity, int, int, int, BlockEntity> setBlock)
    {
        if (y < 0 || y >= ChunkEntity.Height) return;

        if (originChunk.IsInBounds(x, y, z))
        {
            setBlock(originChunk, x, y, z, block);
            return;
        }

        var worldX = originChunk.Position.X + x;
        var worldZ = originChunk.Position.Z + z;
        
        var targetChunk = getChunkAtPosition(new Vector3(worldX, y, worldZ));
        if (targetChunk == null) return;

        var localX = (int)(worldX - targetChunk.Position.X);
        var localZ = (int)(worldZ - targetChunk.Position.Z);

        setBlock(targetChunk, localX, y, localZ, block);
    }

    private void QueueNeighbors(ChunkEntity chunk, int x, int y, int z)
    {
        QueueWaterUpdate(chunk, x, y - 1, z);
        QueueWaterUpdate(chunk, x + 1, y, z);
        QueueWaterUpdate(chunk, x - 1, y, z);
        QueueWaterUpdate(chunk, x, y, z + 1);
        QueueWaterUpdate(chunk, x, y, z - 1);
    }

    private static long _nextId = 1000000000L;
    private static long GenerateId() => System.Threading.Interlocked.Increment(ref _nextId);

    public void Clear()
    {
        _waterUpdateQueue.Clear();
        _queuedPositions.Clear();
    }
}
