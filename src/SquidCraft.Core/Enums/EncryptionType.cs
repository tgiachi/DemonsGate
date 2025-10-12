namespace SquidCraft.Core.Enums;

/// <summary>
///     Defines the types of encryption algorithms supported
/// </summary>
public enum EncryptionType
{
    /// <summary>No encryption</summary>
    None,

    /// <summary>AES-256 encryption algorithm</summary>
    AES256,

    /// <summary>ChaCha20-Poly1305 encryption algorithm</summary>
    ChaCha20Poly1305
}
