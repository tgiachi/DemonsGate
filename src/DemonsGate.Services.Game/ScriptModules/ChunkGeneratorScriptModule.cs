using System.Numerics;
using DemonsGate.Core.Attributes.Scripts;
using DemonsGate.Game.Data.Primitives;
using DemonsGate.Game.Data.Types;
using DemonsGate.Services.Game.Interfaces.Pipeline;
using Serilog;

namespace DemonsGate.Services.Game.ScriptModules;

/// <summary>
/// Lua script module that exposes chunk generation pipeline API to scripts.
/// </summary>
[ScriptModule("chunk_generator")]
public class ChunkGeneratorScriptModule
{
    private readonly ILogger _logger = Log.ForContext<ChunkGeneratorScriptModule>();

    /// <summary>
    /// Gets a block from the chunk at the specified coordinates.
    /// </summary>
    [ScriptFunction("get_block")]
    public object? GetBlock(IGeneratorContext context, int x, int y, int z)
    {
        try
        {
            var block = context.Chunk.GetBlock(x, y, z);
            return new
            {
                id = block.Id,
                block_type = (int)block.BlockType
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting block at ({X}, {Y}, {Z})", x, y, z);
            return null;
        }
    }

    /// <summary>
    /// Sets a block in the chunk at the specified coordinates.
    /// </summary>
    [ScriptFunction("set_block")]
    public void SetBlock(IGeneratorContext context, int x, int y, int z, int blockType, long? blockId = null)
    {
        try
        {
            var id = blockId ?? GenerateBlockId(context, x, y, z);
            var block = new BlockEntity(id, (BlockType)blockType);
            context.Chunk.SetBlock(x, y, z, block);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error setting block at ({X}, {Y}, {Z})", x, y, z);
            throw;
        }
    }

    /// <summary>
    /// Gets noise value at the specified world coordinates.
    /// </summary>
    [ScriptFunction("get_noise")]
    public float GetNoise(IGeneratorContext context, float x, float z)
    {
        return context.NoiseGenerator.GetNoise(x, z);
    }

    /// <summary>
    /// Gets 3D noise value at the specified world coordinates.
    /// </summary>
    [ScriptFunction("get_noise_3d")]
    public float GetNoise3D(IGeneratorContext context, float x, float y, float z)
    {
        return context.NoiseGenerator.GetNoise(x, y, z);
    }

    /// <summary>
    /// Gets the chunk world position.
    /// </summary>
    [ScriptFunction("get_world_position")]
    public object GetWorldPosition(IGeneratorContext context)
    {
        return new
        {
            x = context.WorldPosition.X,
            y = context.WorldPosition.Y,
            z = context.WorldPosition.Z
        };
    }

    /// <summary>
    /// Gets the chunk size (width/depth).
    /// </summary>
    [ScriptFunction("get_chunk_size")]
    public int GetChunkSize()
    {
        return ChunkEntity.Size;
    }

    /// <summary>
    /// Gets the chunk height.
    /// </summary>
    [ScriptFunction("get_chunk_height")]
    public int GetChunkHeight()
    {
        return ChunkEntity.Height;
    }

    /// <summary>
    /// Gets the generation seed.
    /// </summary>
    [ScriptFunction("get_seed")]
    public int GetSeed(IGeneratorContext context)
    {
        return context.Seed;
    }

    /// <summary>
    /// Sets custom data in the context.
    /// </summary>
    [ScriptFunction("set_custom_data")]
    public void SetCustomData(IGeneratorContext context, string key, object value)
    {
        context.CustomData[key] = value;
    }

    /// <summary>
    /// Gets custom data from the context.
    /// </summary>
    [ScriptFunction("get_custom_data")]
    public object? GetCustomData(IGeneratorContext context, string key)
    {
        return context.CustomData.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Logs a message from Lua scripts.
    /// </summary>
    [ScriptFunction("log")]
    public void LogMessage(string message)
    {
        _logger.Information("[Lua Generator] {Message}", message);
    }

    /// <summary>
    /// Generates a block ID based on world position and local coordinates.
    /// </summary>
    private static long GenerateBlockId(IGeneratorContext context, int x, int y, int z)
    {
        var worldPos = context.WorldPosition;
        long wx = (long)worldPos.X;
        long wy = (long)worldPos.Y;
        long wz = (long)worldPos.Z;

        return (wx << 48) | (wy << 32) | (wz << 16) | ((long)x << 12) | ((long)y << 6) | (long)z;
    }
}
