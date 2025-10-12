using System.Security.Cryptography;
using SquidCraft.Core.Enums;

namespace SquidCraft.Core.Utils;

/// <summary>
///     Provides utility methods for encrypting and decrypting data using various algorithms.
/// </summary>
public static class EncryptionUtils
{
    private const int AesKeySize = 32; // 256 bits
    private const int AesIvSize = 16; // 128 bits
    private const int ChaCha20KeySize = 32; // 256 bits
    private const int ChaCha20NonceSize = 12; // 96 bits
    private const int ChaCha20TagSize = 16; // 128 bits

    /// <summary>
    ///     Encrypts data using the specified encryption algorithm.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <param name="encryptionType">The encryption algorithm to use.</param>
    /// <returns>A byte array containing the encrypted data (including IV/nonce and tag if applicable).</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported encryption type is specified or key size is invalid.</exception>
    public static byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key, EncryptionType encryptionType)
    {
        return encryptionType switch
        {
            EncryptionType.None => data.ToArray(),
            EncryptionType.AES256 => EncryptAES256(data, key),
            EncryptionType.ChaCha20Poly1305 => EncryptChaCha20Poly1305(data, key),
            _ => throw new ArgumentException($"Unsupported encryption type: {encryptionType}", nameof(encryptionType))
        };
    }

    /// <summary>
    ///     Decrypts data using the specified encryption algorithm.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt (including IV/nonce and tag if applicable).</param>
    /// <param name="key">The decryption key.</param>
    /// <param name="encryptionType">The encryption algorithm that was used.</param>
    /// <returns>A byte array containing the decrypted data.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported encryption type is specified.</exception>
    /// <exception cref="CryptographicException">Thrown when decryption fails (e.g., wrong key or corrupted data).</exception>
    public static byte[] Decrypt(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key, EncryptionType encryptionType)
    {
        return encryptionType switch
        {
            EncryptionType.None => encryptedData.ToArray(),
            EncryptionType.AES256 => DecryptAES256(encryptedData, key),
            EncryptionType.ChaCha20Poly1305 => DecryptChaCha20Poly1305(encryptedData, key),
            _ => throw new ArgumentException($"Unsupported encryption type: {encryptionType}", nameof(encryptionType))
        };
    }

    /// <summary>
    ///     Generates a cryptographically secure random key for the specified encryption type.
    /// </summary>
    /// <param name="encryptionType">The encryption type to generate a key for.</param>
    /// <returns>A byte array containing the generated key.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported encryption type is specified.</exception>
    public static byte[] GenerateKey(EncryptionType encryptionType)
    {
        var keySize = encryptionType switch
        {
            EncryptionType.None => 0,
            EncryptionType.AES256 => AesKeySize,
            EncryptionType.ChaCha20Poly1305 => ChaCha20KeySize,
            _ => throw new ArgumentException($"Unsupported encryption type: {encryptionType}", nameof(encryptionType))
        };

        if (keySize == 0)
            return Array.Empty<byte>();

        var key = new byte[keySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    /// <summary>
    ///     Encrypts data using AES-256 in CBC mode.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key (must be 32 bytes for AES-256).</param>
    /// <returns>A byte array containing the IV (16 bytes) followed by the encrypted data.</returns>
    /// <exception cref="ArgumentException">Thrown when the key size is invalid.</exception>
    private static byte[] EncryptAES256(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        if (key.Length != AesKeySize)
            throw new ArgumentException($"AES-256 requires a {AesKeySize}-byte key", nameof(key));

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var cipherBytes = encryptor.TransformFinalBlock(data.ToArray(), 0, data.Length);

        // IV (16 bytes) + ciphertext
        var result = new byte[AesIvSize + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, AesIvSize);
        return result;
    }

    /// <summary>
    ///     Decrypts AES-256 encrypted data.
    /// </summary>
    /// <param name="encryptedData">The encrypted data (IV + ciphertext).</param>
    /// <param name="key">The decryption key (must be 32 bytes for AES-256).</param>
    /// <returns>A byte array containing the decrypted data.</returns>
    /// <exception cref="ArgumentException">Thrown when the key size or encrypted data size is invalid.</exception>
    private static byte[] DecryptAES256(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key)
    {
        if (key.Length != AesKeySize)
            throw new ArgumentException($"AES-256 requires a {AesKeySize}-byte key", nameof(key));

        if (encryptedData.Length < AesIvSize)
            throw new ArgumentException("Encrypted data is too short", nameof(encryptedData));

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = encryptedData[..AesIvSize].ToArray();

        var cipherBytes = encryptedData[AesIvSize..].ToArray();

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
    }

    /// <summary>
    ///     Encrypts data using ChaCha20-Poly1305 AEAD.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key (must be 32 bytes).</param>
    /// <returns>A byte array containing the nonce (12 bytes), tag (16 bytes), and ciphertext.</returns>
    /// <exception cref="ArgumentException">Thrown when the key size is invalid.</exception>
    private static byte[] EncryptChaCha20Poly1305(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        if (key.Length != ChaCha20KeySize)
            throw new ArgumentException($"ChaCha20-Poly1305 requires a {ChaCha20KeySize}-byte key", nameof(key));

        using var cipher = new ChaCha20Poly1305(key);

        var nonce = new byte[ChaCha20NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[data.Length];
        var tag = new byte[ChaCha20TagSize];

        cipher.Encrypt(nonce, data, ciphertext, tag);

        // nonce (12 bytes) + tag (16 bytes) + ciphertext
        var result = new byte[ChaCha20NonceSize + ChaCha20TagSize + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, ChaCha20NonceSize);
        ciphertext.CopyTo(result, ChaCha20NonceSize + ChaCha20TagSize);
        return result;
    }

    /// <summary>
    ///     Decrypts ChaCha20-Poly1305 encrypted data.
    /// </summary>
    /// <param name="encryptedData">The encrypted data (nonce + tag + ciphertext).</param>
    /// <param name="key">The decryption key (must be 32 bytes).</param>
    /// <returns>A byte array containing the decrypted data.</returns>
    /// <exception cref="ArgumentException">Thrown when the key size or encrypted data size is invalid.</exception>
    /// <exception cref="CryptographicException">Thrown when authentication fails.</exception>
    private static byte[] DecryptChaCha20Poly1305(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> key)
    {
        if (key.Length != ChaCha20KeySize)
            throw new ArgumentException($"ChaCha20-Poly1305 requires a {ChaCha20KeySize}-byte key", nameof(key));

        var minSize = ChaCha20NonceSize + ChaCha20TagSize;
        if (encryptedData.Length < minSize)
            throw new ArgumentException($"Encrypted data must be at least {minSize} bytes", nameof(encryptedData));

        using var cipher = new ChaCha20Poly1305(key);

        var nonce = encryptedData[..ChaCha20NonceSize];
        var tag = encryptedData.Slice(ChaCha20NonceSize, ChaCha20TagSize);
        var ciphertext = encryptedData[(ChaCha20NonceSize + ChaCha20TagSize)..];

        var plaintext = new byte[ciphertext.Length];
        cipher.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
