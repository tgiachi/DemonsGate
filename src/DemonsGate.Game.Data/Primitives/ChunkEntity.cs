using System.Numerics;

namespace DemonsGate.Game.Data.Primitives;

public class ChunkEntity
{
    public const int Size = 16;
    public const int Height = 64;

    public Vector3 Position { get; }

    public ChunkEntity(Vector3 position)
    {
        Blocks = new BlockEntity[Size * Size * Height];
        Position = position;
    }

    public BlockEntity[] Blocks { get; }

    public BlockEntity GetBlock(int x, int y, int z)
    {
        return Blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, BlockEntity block)
    {
        Blocks[GetIndex(x, y, z)] = block ?? throw new ArgumentNullException(nameof(block));
    }

    public BlockEntity GetBlock(Vector3 position)
    {
        return GetBlock((int)position.X, (int)position.Y, (int)position.Z);
    }

    public void SetBlock(Vector3 position, BlockEntity block)
    {
        SetBlock((int)position.X, (int)position.Y, (int)position.Z, block);
    }

    public BlockEntity GetBlock(int index)
    {
        ValidateIndex(index);
        return Blocks[index];
    }

    public void SetBlock(int index, BlockEntity block)
    {
        ValidateIndex(index);
        Blocks[index] = block;
    }

    public int GetIndex(int x, int y, int z)
    {
        ValidateCoordinates(x, y, z);
        return x + y * Size + z * Size * Height;
    }

    public int GetIndex(Vector3 position)
    {
        return GetIndex((int)position.X, (int)position.Y, (int)position.Z);
    }


    public BlockEntity this[int x, int y, int z]
    {
        get => GetBlock(x, y, z);
        set => SetBlock(x, y, z, value);
    }

    public BlockEntity this[Vector3 position]
    {
        get => GetBlock(position);
        set => SetBlock(position, value);
    }

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

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Blocks.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Expected 0 <= index < {Blocks.Length}.");
        }
    }
}
