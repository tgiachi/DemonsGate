using System;
using System.Numerics;

namespace SquidCraft.Game.Data.Primitives;

/// <summary>
/// Stores the runtime state for a cubic chunk, including its position and contained blocks.
/// </summary>
public class ChunkEntity
{
    /// <summary>
    /// Number of blocks along the X and Z axes.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// Number of blocks along the Y axis.
    /// </summary>
    public const int Height = 64;

    /// <summary>
    /// Gets the world position at which the chunk is anchored.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Initializes a new <see cref="ChunkEntity"/> at the provided position.
    /// </summary>
    /// <param name="position">World position of the chunk origin.</param>
    public ChunkEntity(Vector3 position)
    {
        Blocks = new BlockEntity[Size * Size * Height];
        Position = position;
    }

    /// <summary>
    /// Gets the raw backing array that stores blocks for the chunk.
    /// </summary>
    public BlockEntity[] Blocks { get; }

    /// <summary>
    /// Retrieves the block stored at the specified coordinates.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <returns>The block entity at the given coordinates.</returns>
    public BlockEntity GetBlock(int x, int y, int z)
    {
        return Blocks[GetIndex(x, y, z)];
    }

    /// <summary>
    /// Stores a block at the specified coordinates, replacing any previous value.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <param name="block">Block entity to store.</param>
    public void SetBlock(int x, int y, int z, BlockEntity block)
    {
        Blocks[GetIndex(x, y, z)] = block ?? throw new ArgumentNullException(nameof(block));
    }

    /// <summary>
    /// Retrieves the block at the specified vector position.
    /// </summary>
    /// <param name="position">Vector position of the block.</param>
    /// <returns>The block entity at the position.</returns>
    public BlockEntity GetBlock(Vector3 position)
    {
        return GetBlock((int)position.X, (int)position.Y, (int)position.Z);
    }

    /// <summary>
    /// Stores a block at the position represented by the vector.
    /// </summary>
    /// <param name="position">Vector position of the block.</param>
    /// <param name="block">Block entity to store.</param>
    public void SetBlock(Vector3 position, BlockEntity block)
    {
        SetBlock((int)position.X, (int)position.Y, (int)position.Z, block);
    }

    /// <summary>
    /// Retrieves the block stored at the specified linear index.
    /// </summary>
    /// <param name="index">Zero-based linear index into the chunk.</param>
    /// <returns>The block entity at the given index.</returns>
    public BlockEntity GetBlock(int index)
    {
        ValidateIndex(index);
        return Blocks[index];
    }

    /// <summary>
    /// Stores a block at the specified linear index.
    /// </summary>
    /// <param name="index">Zero-based linear index.</param>
    /// <param name="block">Block entity to store.</param>
    public void SetBlock(int index, BlockEntity block)
    {
        ValidateIndex(index);
        Blocks[index] = block ?? throw new ArgumentNullException(nameof(block));
    }

    /// <summary>
    /// Calculates the linear index for the provided block coordinates.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <returns>The corresponding linear index.</returns>
    public static int GetIndex(int x, int y, int z)
    {
        ValidateCoordinates(x, y, z);
        return x + y * Size + z * Size * Height;
    }

    /// <summary>
    /// Calculates the linear index for the provided vector position.
    /// </summary>
    /// <param name="position">Vector position of the block.</param>
    /// <returns>The corresponding linear index.</returns>
    public int GetIndex(Vector3 position)
    {
        return GetIndex((int)position.X, (int)position.Y, (int)position.Z);
    }


    /// <summary>
    /// Provides array-style access to blocks using explicit coordinates.
    /// </summary>
    public BlockEntity this[int x, int y, int z]
    {
        get => GetBlock(x, y, z);
        set => SetBlock(x, y, z, value);
    }

    /// <summary>
    /// Provides array-style access to blocks using a vector position.
    /// </summary>
    public BlockEntity this[Vector3 position]
    {
        get => GetBlock(position);
        set => SetBlock(position, value);
    }

    /// <summary>
    /// Validates that the provided coordinates fall within chunk bounds.
    /// </summary>
    /// <param name="x">Block coordinate along X.</param>
    /// <param name="y">Block coordinate along Y.</param>
    /// <param name="z">Block coordinate along Z.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any coordinate is outside the chunk dimensions.</exception>
    private static void ValidateCoordinates(int x, int y, int z)
    {
        if ((uint)x >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"Expected 0 <= x < {Size}.");
        }

        if ((uint)y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Expected 0 <= y < {Height}.");
        }

        if ((uint)z >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(z), z, $"Expected 0 <= z < {Size}.");
        }
    }

    /// <summary>
    /// Validates that the provided index falls within the bounds of the chunk.
    /// </summary>
    /// <param name="index">Linear index into the block array.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is outside the chunk range.</exception>
    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Blocks.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Expected 0 <= index < {Blocks.Length}.");
        }
    }
}
