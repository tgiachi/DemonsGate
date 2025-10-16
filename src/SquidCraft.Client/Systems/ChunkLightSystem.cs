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
    private const byte MinAmbientLight = 2; // Minimum ambient light level

    // Cache for block opacity to avoid repeated BlockManagerService calls
    // Opacity values: 1 = transparent (air/leaves), 2 = semi-transparent (water), 15 = opaque (solid blocks)
    private static readonly Dictionary<BlockType, byte> BlockOpacity = new()
    {
        { BlockType.Air, 1 },
        { BlockType.Stone, 15 },
        { BlockType.Dirt, 15 },
        { BlockType.Grass, 15 },
        { BlockType.Bedrock, 15 },
        { BlockType.Snow, 2 },
        { BlockType.Ice, 1 },
        { BlockType.Moss, 15 },
        { BlockType.Wood, 15 },
        { BlockType.Leaves, 1 },
        { BlockType.Water, 2 },
        { BlockType.TallGrass, 1 },
        { BlockType.Flower, 1 },
    };

    // Light sources and their emission levels
    private static readonly Dictionary<BlockType, byte> LightSources = new()
    {
        { BlockType.Water, 8 }, // Water glows
    };

    // Delegate to get neighboring chunks for cross-chunk lighting
    public delegate ChunkEntity? GetNeighborChunkHandler(int chunkX, int chunkZ);

    public void CalculateInitialSunlight(ChunkEntity chunk)
    {
        // Lazy lighting: only recalculate if dirty
        if (!chunk.IsLightingDirty)
        {
            return;
        }

        var lightLevels = new byte[ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height];

        // Initialize all blocks to 0 light
        Array.Fill(lightLevels, (byte)0);

        // Phase 1: Sunlight propagation from top
        CalculateSunlight(chunk, lightLevels);

        // Phase 2: Block light sources
        CalculateBlockLights(chunk, lightLevels);

        chunk.SetLightLevels(lightLevels);
        chunk.IsLightingDirty = false; // Mark as clean

        _logger.Debug("Calculated lighting for chunk at {Position}", chunk.Position);
    }



    /// <summary>
    /// Invalidates the lighting for a chunk, marking it as dirty so it will be recalculated on next access.
    /// Call this when blocks in the chunk are modified.
    /// </summary>
    public void InvalidateLighting(ChunkEntity chunk)
    {
        chunk.IsLightingDirty = true;
        _logger.Debug("Invalidated lighting for chunk at {Position}", chunk.Position);
    }

    public void CalculateCrossChunkLighting(IEnumerable<ChunkEntity> chunks, GetNeighborChunkHandler getNeighborChunk)
    {
        // Create a map of chunk positions to chunks for easy lookup
        var chunkMap = chunks.ToDictionary(c => (c.Position.X / ChunkEntity.Size, c.Position.Z / ChunkEntity.Size), c => c);

        // Initialize all light levels to 0
        foreach (var chunk in chunks)
        {
            var lightLevels = new byte[ChunkEntity.Size * ChunkEntity.Size * ChunkEntity.Height];
            Array.Fill(lightLevels, (byte)0);
            chunk.SetLightLevels(lightLevels);
        }

        // Phase 1: Sunlight propagation across all chunks
        CalculateCrossChunkSunlight(chunks, chunkMap, getNeighborChunk);

        // Phase 2: Block light sources across all chunks
        CalculateCrossChunkBlockLights(chunks, chunkMap, getNeighborChunk);

        _logger.Debug("Calculated cross-chunk lighting for {Count} chunks", chunks.Count());
    }

    private void CalculateCrossChunkSunlight(
        IEnumerable<ChunkEntity> chunks, Dictionary<(float, float), ChunkEntity> chunkMap,
        GetNeighborChunkHandler getNeighborChunk
    )
    {
        // For cross-chunk sunlight, we need to process columns that span multiple chunks
        // This is complex, so for now we'll calculate sunlight per chunk but allow it to propagate to neighbors
        foreach (var chunk in chunks)
        {
            var lightLevels = chunk.LightLevels;
            CalculateSunlight(chunk, lightLevels);

            // Now propagate sunlight to neighboring chunks where appropriate
            PropagateSunlightToNeighbors(chunk, chunkMap, getNeighborChunk);
        }
    }

    private void PropagateSunlightToNeighbors(
        ChunkEntity chunk, Dictionary<(float, float), ChunkEntity> chunkMap, GetNeighborChunkHandler getNeighborChunk
    )
    {
        var chunkX = (int)(chunk.Position.X / ChunkEntity.Size);
        var chunkZ = (int)(chunk.Position.Z / ChunkEntity.Size);

        // Check all 4 neighbors
        var neighbors = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };

        foreach (var (dx, dz) in neighbors)
        {
            var neighborX = chunkX + dx;
            var neighborZ = chunkZ + dz;

            if (chunkMap.TryGetValue((neighborX, neighborZ), out var neighborChunk))
            {
                // Propagate light across the boundary
                PropagateLightAcrossBoundary(chunk, neighborChunk, dx, dz);
            }
        }
    }

    private void PropagateLightAcrossBoundary(ChunkEntity fromChunk, ChunkEntity toChunk, int dx, int dz)
    {
        // Determine which faces are adjacent
        int fromStartX, fromEndX, fromStartZ, fromEndZ;
        int toStartX, toEndX, toStartZ, toEndZ;

        if (dx == 1) // fromChunk is west of toChunk
        {
            fromStartX = ChunkEntity.Size - 1;
            fromEndX = ChunkEntity.Size - 1;
            toStartX = 0;
            toEndX = 0;
            fromStartZ = 0;
            fromEndZ = ChunkEntity.Size - 1;
            toStartZ = 0;
            toEndZ = ChunkEntity.Size - 1;
        }
        else if (dx == -1) // fromChunk is east of toChunk
        {
            fromStartX = 0;
            fromEndX = 0;
            toStartX = ChunkEntity.Size - 1;
            toEndX = ChunkEntity.Size - 1;
            fromStartZ = 0;
            fromEndZ = ChunkEntity.Size - 1;
            toStartZ = 0;
            toEndZ = ChunkEntity.Size - 1;
        }
        else if (dz == 1) // fromChunk is north of toChunk
        {
            fromStartZ = ChunkEntity.Size - 1;
            fromEndZ = ChunkEntity.Size - 1;
            toStartZ = 0;
            toEndZ = 0;
            fromStartX = 0;
            fromEndX = ChunkEntity.Size - 1;
            toStartX = 0;
            toEndX = ChunkEntity.Size - 1;
        }
        else // dz == -1, fromChunk is south of toChunk
        {
            fromStartZ = 0;
            fromEndZ = 0;
            toStartZ = ChunkEntity.Size - 1;
            toEndZ = ChunkEntity.Size - 1;
            fromStartX = 0;
            fromEndX = ChunkEntity.Size - 1;
            toStartX = 0;
            toEndX = ChunkEntity.Size - 1;
        }

        var fromLightLevels = fromChunk.LightLevels;
        var toLightLevels = toChunk.LightLevels;

        // Propagate light from boundary blocks
        for (int x = fromStartX; x <= fromEndX; x++)
        {
            for (int z = fromStartZ; z <= fromEndZ; z++)
            {
                for (int y = 0; y < ChunkEntity.Height; y++)
                {
                    var fromIndex = ChunkEntity.GetIndex(x, y, z);
                    var fromLight = fromLightLevels[fromIndex];

                    if (fromLight > 1)
                    {
                        var toX = dx == 0 ? x : (dx == 1 ? toStartX : toEndX);
                        var toZ = dz == 0 ? z : (dz == 1 ? toStartZ : toEndZ);
                        var toIndex = ChunkEntity.GetIndex(toX, y, toZ);

                        var toBlock = toChunk.GetBlock(toX, y, toZ);
                        byte reduction = 1;
                        if (toBlock != null && BlockOpacity.TryGetValue(toBlock.BlockType, out var opacity))
                        {
                            reduction = opacity;
                        }

                        var propagatedLight = (byte)Math.Max(0, fromLight - reduction);
                        if (propagatedLight > toLightLevels[toIndex])
                        {
                            toLightLevels[toIndex] = propagatedLight;
                        }
                    }
                }
            }
        }
    }

    private void CalculateCrossChunkBlockLights(
        IEnumerable<ChunkEntity> chunks, Dictionary<(float, float), ChunkEntity> chunkMap,
        GetNeighborChunkHandler getNeighborChunk
    )
    {
        foreach (var chunk in chunks)
        {
            var lightLevels = chunk.LightLevels;
            CalculateBlockLights(chunk, lightLevels);
        }

        // After calculating block lights, propagate them across chunk boundaries
        foreach (var chunk in chunks)
        {
            PropagateBlockLightsToNeighbors(chunk, chunkMap, getNeighborChunk);
        }
    }

    private void PropagateBlockLightsToNeighbors(
        ChunkEntity chunk, Dictionary<(float, float), ChunkEntity> chunkMap, GetNeighborChunkHandler getNeighborChunk
    )
    {
        var chunkX = (int)(chunk.Position.X / ChunkEntity.Size);
        var chunkZ = (int)(chunk.Position.Z / ChunkEntity.Size);

        // Check all 4 neighbors
        var neighbors = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };

        foreach (var (dx, dz) in neighbors)
        {
            var neighborX = chunkX + dx;
            var neighborZ = chunkZ + dz;

            if (chunkMap.TryGetValue((neighborX, neighborZ), out var neighborChunk))
            {
                // Propagate block lights across the boundary (uses same method as sunlight)
                PropagateLightAcrossBoundary(chunk, neighborChunk, dx, dz);
            }
        }
    }



    private void CalculateSunlight(ChunkEntity chunk, byte[] lightLevels)
    {
        // Sunlight comes from the top (Y = Height - 1) with full intensity
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int z = 0; z < ChunkEntity.Size; z++)
            {
                byte currentLight = MaxLightLevel; // Start with full sunlight

                for (int y = ChunkEntity.Height - 1; y >= 0; y--)
                {
                    var index = ChunkEntity.GetIndex(x, y, z);
                    var block = chunk.Blocks[index];

                    if (block == null || block.BlockType == BlockType.Air)
                    {
                        // Air blocks get current light level, but never below ambient
                        lightLevels[index] = Math.Max(lightLevels[index], Math.Max(currentLight, MinAmbientLight));
                    }
                    else
                    {
                        // Solid blocks get current light but reduce it for blocks below
                        lightLevels[index] = Math.Max(lightLevels[index], Math.Max(currentLight, MinAmbientLight));

                        // Use cached opacity for performance
                        byte opacity = BlockOpacity.TryGetValue(block.BlockType, out var op) ? op : (byte)15;
                        
                        if (opacity <= 2)
                        {
                            // Transparent/semi-transparent blocks reduce light slightly
                            currentLight = (byte)Math.Max(MinAmbientLight, currentLight - opacity);
                        }
                        else
                        {
                            // Solid blocks reduce light significantly but maintain some ambient light
                            currentLight = MinAmbientLight;
                        }
                    }

                    // Continue propagation even with low light for ambient effect
                    // Only stop if we're at minimum ambient light
                    if (currentLight <= MinAmbientLight) break;
                }
            }
        }
    }

    private void CalculateBlockLights(ChunkEntity chunk, byte[] lightLevels)
    {
        // Find all light sources and propagate their light
        for (int x = 0; x < ChunkEntity.Size; x++)
        {
            for (int y = 0; y < ChunkEntity.Height; y++)
            {
                for (int z = 0; z < ChunkEntity.Size; z++)
                {
                    var index = ChunkEntity.GetIndex(x, y, z);
                    var block = chunk.Blocks[index];

                    if (block != null && LightSources.TryGetValue(block.BlockType, out var emissionLevel))
                    {
                        // This block emits light - propagate it
                        PropagateLightFromSource(chunk, lightLevels, x, y, z, emissionLevel);
                    }
                }
            }
        }
    }

    private void PropagateLightFromSource(
        ChunkEntity chunk, byte[] lightLevels, int startX, int startY, int startZ, byte startLight
    )
    {
        var queue = new PriorityQueue<(int x, int y, int z, byte light), int>();
        var visited = new HashSet<(int, int, int)>();

        queue.Enqueue((startX, startY, startZ, startLight), -startLight); // Higher light = higher priority
        visited.Add((startX, startY, startZ));

        // Set the source block light
        var sourceIndex = ChunkEntity.GetIndex(startX, startY, startZ);
        lightLevels[sourceIndex] = Math.Max(lightLevels[sourceIndex], startLight);

        while (queue.Count > 0)
        {
            var (x, y, z, light) = queue.Dequeue();

            if (light <= 1) continue; // No more light to propagate

            // Check all 6 neighbors
            var neighbors = new[]
            {
                (x + 1, y, z), (x - 1, y, z),
                (x, y + 1, z), (x, y - 1, z),
                (x, y, z + 1), (x, y, z - 1)
            };

            foreach (var (nx, ny, nz) in neighbors)
            {
                if (!chunk.IsInBounds(nx, ny, nz) || visited.Contains((nx, ny, nz)))
                    continue;

                var neighborIndex = ChunkEntity.GetIndex(nx, ny, nz);
                var neighborBlock = chunk.Blocks[neighborIndex];

                // Calculate light reduction based on block type using cached opacity
                byte reduction = 1; // Default for air
                if (neighborBlock != null && BlockOpacity.TryGetValue(neighborBlock.BlockType, out var opacity))
                {
                    reduction = opacity;
                }

                var newLight = (byte)Math.Max(0, light - reduction);

                if (newLight > lightLevels[neighborIndex])
                {
                    lightLevels[neighborIndex] = newLight;
                    visited.Add((nx, ny, nz));
                    queue.Enqueue((nx, ny, nz, newLight), -newLight);
                }
            }
        }
    }
}
