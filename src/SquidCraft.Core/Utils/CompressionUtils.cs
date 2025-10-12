using System.IO.Compression;
using SquidCraft.Core.Enums;
using K4os.Compression.LZ4;

namespace SquidCraft.Core.Utils;

/// <summary>
///     Provides utility methods for compressing and decompressing Span data using various algorithms.
/// </summary>
public static class CompressionUtils
{
    /// <summary>
    ///     Compresses data using the specified compression algorithm.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="compressionType">The compression algorithm to use.</param>
    /// <returns>A byte array containing the compressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported compression type is specified.</exception>
    public static byte[] Compress(ReadOnlySpan<byte> data, CompressionType compressionType)
    {
        return compressionType switch
        {
            CompressionType.None => data.ToArray(),
            CompressionType.Brotli => CompressBrotli(data),
            CompressionType.GZip => CompressGZip(data),
            CompressionType.Deflate => CompressDeflate(data),
            CompressionType.LZ4 => CompressLZ4(data),
            _ => throw new ArgumentException($"Unsupported compression type: {compressionType}", nameof(compressionType))
        };
    }

    /// <summary>
    ///     Decompresses data using the specified compression algorithm.
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <param name="compressionType">The compression algorithm that was used.</param>
    /// <returns>A byte array containing the decompressed data.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported compression type is specified.</exception>
    public static byte[] Decompress(ReadOnlySpan<byte> compressedData, CompressionType compressionType)
    {
        return compressionType switch
        {
            CompressionType.None => compressedData.ToArray(),
            CompressionType.Brotli => DecompressBrotli(compressedData),
            CompressionType.GZip => DecompressGZip(compressedData),
            CompressionType.Deflate => DecompressDeflate(compressedData),
            CompressionType.LZ4 => DecompressLZ4(compressedData),
            _ => throw new ArgumentException($"Unsupported compression type: {compressionType}", nameof(compressionType))
        };
    }

    /// <summary>
    ///     Compresses data using the Brotli algorithm.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>A byte array containing the compressed data.</returns>
    private static byte[] CompressBrotli(ReadOnlySpan<byte> data)
    {
        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, CompressionLevel.Optimal))
        {
            brotliStream.Write(data);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Decompresses data using the Brotli algorithm.
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <returns>A byte array containing the decompressed data.</returns>
    private static byte[] DecompressBrotli(ReadOnlySpan<byte> compressedData)
    {
        using var inputStream = new MemoryStream(compressedData.ToArray());
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        brotliStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Compresses data using the GZip algorithm.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>A byte array containing the compressed data.</returns>
    private static byte[] CompressGZip(ReadOnlySpan<byte> data)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            gzipStream.Write(data);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Decompresses data using the GZip algorithm.
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <returns>A byte array containing the decompressed data.</returns>
    private static byte[] DecompressGZip(ReadOnlySpan<byte> compressedData)
    {
        using var inputStream = new MemoryStream(compressedData.ToArray());
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Compresses data using the Deflate algorithm.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>A byte array containing the compressed data.</returns>
    private static byte[] CompressDeflate(ReadOnlySpan<byte> data)
    {
        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
        {
            deflateStream.Write(data);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Decompresses data using the Deflate algorithm.
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <returns>A byte array containing the decompressed data.</returns>
    private static byte[] DecompressDeflate(ReadOnlySpan<byte> compressedData)
    {
        using var inputStream = new MemoryStream(compressedData.ToArray());
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        deflateStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    ///     Compresses data using the LZ4 algorithm.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>A byte array containing the compressed data.</returns>
    private static byte[] CompressLZ4(ReadOnlySpan<byte> data)
    {
        var maxCompressedLength = LZ4Codec.MaximumOutputSize(data.Length);
        var compressedBuffer = new byte[maxCompressedLength];
        var compressedLength = LZ4Codec.Encode(data, compressedBuffer, LZ4Level.L12_MAX);

        var result = new byte[compressedLength];
        Array.Copy(compressedBuffer, result, compressedLength);
        return result;
    }

    /// <summary>
    ///     Decompresses data using the LZ4 algorithm.
    /// </summary>
    /// <param name="compressedData">The compressed data to decompress.</param>
    /// <returns>A byte array containing the decompressed data.</returns>
    /// <remarks>
    ///     For LZ4 decompression, the original size must be known. This implementation uses
    ///     a trial-and-error approach with increasing buffer sizes.
    ///     For production use, consider storing the original size alongside the compressed data.
    /// </remarks>
    private static byte[] DecompressLZ4(ReadOnlySpan<byte> compressedData)
    {
        // Try to decode with increasingly larger buffers
        // Start with a reasonable multiplier (LZ4 typically achieves 2-3x compression)
        var outputBuffer = new byte[compressedData.Length * 4];

        try
        {
            var decompressedLength = LZ4Codec.Decode(compressedData, outputBuffer);
            var result = new byte[decompressedLength];
            Array.Copy(outputBuffer, result, decompressedLength);
            return result;
        }
        catch
        {
            // If the buffer was too small, try with a larger one
            outputBuffer = new byte[compressedData.Length * 10];
            var decompressedLength = LZ4Codec.Decode(compressedData, outputBuffer);
            var result = new byte[decompressedLength];
            Array.Copy(outputBuffer, result, decompressedLength);
            return result;
        }
    }
}
