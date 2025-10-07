using System.Text;
using DemonsGate.Core.Enums;
using DemonsGate.Core.Utils;

namespace DemonsGate.Tests.Core.Utils;

[TestFixture]
/// <summary>
/// Contains test cases for compression utilities.
/// </summary>
public class CompressionUtilsTests
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Hello, World! This is a test compression string with some repeated data. Hello, World!");

    [Test]
    [TestCase(CompressionType.None)]
    [TestCase(CompressionType.Brotli)]
    [TestCase(CompressionType.GZip)]
    [TestCase(CompressionType.Deflate)]
    [TestCase(CompressionType.LZ4)]
    public void Compress_ShouldCompressData(CompressionType compressionType)
    {
        // Act
        var compressed = CompressionUtils.Compress(TestData, compressionType);

        // Assert
        Assert.That(compressed, Is.Not.Null);
        Assert.That(compressed, Has.Length.GreaterThan(0));

        if (compressionType == CompressionType.None)
        {
            Assert.That(compressed, Is.EqualTo(TestData));
        }
    }

    [Test]
    [TestCase(CompressionType.Brotli)]
    [TestCase(CompressionType.GZip)]
    [TestCase(CompressionType.Deflate)]
    [TestCase(CompressionType.LZ4)]
    public void Compress_ShouldReduceDataSize(CompressionType compressionType)
    {
        // Arrange - Create highly compressible data
        var repetitiveData = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("AAAABBBBCCCCDDDD", 100)));

        // Act
        var compressed = CompressionUtils.Compress(repetitiveData, compressionType);

        // Assert
        Assert.That(compressed.Length, Is.LessThan(repetitiveData.Length));
    }

    [Test]
    [TestCase(CompressionType.None)]
    [TestCase(CompressionType.Brotli)]
    [TestCase(CompressionType.GZip)]
    [TestCase(CompressionType.Deflate)]
    [TestCase(CompressionType.LZ4)]
    public void Decompress_ShouldDecompressData(CompressionType compressionType)
    {
        // Arrange
        var compressed = CompressionUtils.Compress(TestData, compressionType);

        // Act
        var decompressed = CompressionUtils.Decompress(compressed, compressionType);

        // Assert
        Assert.That(decompressed, Is.EqualTo(TestData));
    }

    [Test]
    [TestCase(CompressionType.Brotli)]
    [TestCase(CompressionType.GZip)]
    [TestCase(CompressionType.Deflate)]
    [TestCase(CompressionType.LZ4)]
    public void CompressDecompress_RoundTrip_ShouldPreserveData(CompressionType compressionType)
    {
        // Arrange
        var originalData = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. 1234567890!");

        // Act
        var compressed = CompressionUtils.Compress(originalData, compressionType);
        var decompressed = CompressionUtils.Decompress(compressed, compressionType);

        // Assert
        Assert.That(decompressed, Is.EqualTo(originalData));
        Assert.That(Encoding.UTF8.GetString(decompressed), Is.EqualTo("The quick brown fox jumps over the lazy dog. 1234567890!"));
    }

    [Test]
    public void Compress_WithEmptyData_ShouldHandleGracefully()
    {
        // Arrange
        var emptyData = Array.Empty<byte>();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var compressed = CompressionUtils.Compress(emptyData, CompressionType.GZip);
            var decompressed = CompressionUtils.Decompress(compressed, CompressionType.GZip);
            Assert.That(decompressed, Is.Empty);
        });
    }

    [Test]
    public void Compress_WithLargeData_ShouldHandleCorrectly()
    {
        // Arrange
        var largeData = new byte[1024 * 1024]; // 1MB
        Random.Shared.NextBytes(largeData);

        // Act
        var compressed = CompressionUtils.Compress(largeData, CompressionType.LZ4);
        var decompressed = CompressionUtils.Decompress(compressed, CompressionType.LZ4);

        // Assert
        Assert.That(decompressed, Is.EqualTo(largeData));
    }

    [Test]
    [TestCase(CompressionType.Brotli)]
    [TestCase(CompressionType.GZip)]
    [TestCase(CompressionType.Deflate)]
    [TestCase(CompressionType.LZ4)]
    public void Compress_DifferentAlgorithms_ProduceDifferentOutput(CompressionType compressionType)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Compression test data");
        var compressedWithNone = CompressionUtils.Compress(data, CompressionType.None);

        // Act
        var compressed = CompressionUtils.Compress(data, compressionType);

        // Assert
        Assert.That(compressed, Is.Not.EqualTo(compressedWithNone));
    }
}
