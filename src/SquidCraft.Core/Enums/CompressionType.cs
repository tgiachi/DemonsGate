namespace SquidCraft.Core.Enums;

/// <summary>
///     Defines the types of compression algorithms supported
/// </summary>
public enum CompressionType
{
    /// <summary>No compression</summary>
    None,

    /// <summary>Brotli compression algorithm</summary>
    Brotli,

    /// <summary>GZip compression algorithm</summary>
    GZip,

    /// <summary>Deflate compression algorithm</summary>
    Deflate,

    /// <summary>LZ4 compression algorithm</summary>
    LZ4
}
