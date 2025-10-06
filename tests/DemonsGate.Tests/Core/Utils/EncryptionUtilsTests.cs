using System.Security.Cryptography;
using System.Text;
using DemonsGate.Core.Enums;
using DemonsGate.Core.Utils;

namespace DemonsGate.Tests.Core.Utils;

[TestFixture]
public class EncryptionUtilsTests
{
    private static readonly byte[] TestData = Encoding.UTF8.GetBytes("Sensitive data that needs encryption!");

    [Test]
    [TestCase(EncryptionType.None)]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void GenerateKey_ShouldGenerateCorrectKeySize(EncryptionType encryptionType)
    {
        // Act
        var key = EncryptionUtils.GenerateKey(encryptionType);

        // Assert
        var expectedSize = encryptionType switch
        {
            EncryptionType.None => 0,
            EncryptionType.AES256 => 32,
            EncryptionType.ChaCha20Poly1305 => 32,
            _ => throw new ArgumentException("Invalid encryption type")
        };

        Assert.That(key, Has.Length.EqualTo(expectedSize));
    }

    [Test]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void GenerateKey_ShouldGenerateUniqueKeys(EncryptionType encryptionType)
    {
        // Act
        var key1 = EncryptionUtils.GenerateKey(encryptionType);
        var key2 = EncryptionUtils.GenerateKey(encryptionType);

        // Assert
        Assert.That(key1, Is.Not.EqualTo(key2));
    }

    [Test]
    [TestCase(EncryptionType.None)]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void Encrypt_ShouldEncryptData(EncryptionType encryptionType)
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(encryptionType);

        // Act
        var encrypted = EncryptionUtils.Encrypt(TestData, key, encryptionType);

        // Assert
        Assert.That(encrypted, Is.Not.Null);
        Assert.That(encrypted, Has.Length.GreaterThan(0));

        if (encryptionType == EncryptionType.None)
        {
            Assert.That(encrypted, Is.EqualTo(TestData));
        }
        else
        {
            Assert.That(encrypted, Is.Not.EqualTo(TestData));
        }
    }

    [Test]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void Encrypt_WithSameKey_ShouldProduceDifferentCiphertext(EncryptionType encryptionType)
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(encryptionType);

        // Act
        var encrypted1 = EncryptionUtils.Encrypt(TestData, key, encryptionType);
        var encrypted2 = EncryptionUtils.Encrypt(TestData, key, encryptionType);

        // Assert - Should be different due to random IV/nonce
        Assert.That(encrypted1, Is.Not.EqualTo(encrypted2));
    }

    [Test]
    [TestCase(EncryptionType.None)]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void Decrypt_ShouldDecryptData(EncryptionType encryptionType)
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(encryptionType);
        var encrypted = EncryptionUtils.Encrypt(TestData, key, encryptionType);

        // Act
        var decrypted = EncryptionUtils.Decrypt(encrypted, key, encryptionType);

        // Assert
        Assert.That(decrypted, Is.EqualTo(TestData));
    }

    [Test]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void EncryptDecrypt_RoundTrip_ShouldPreserveData(EncryptionType encryptionType)
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(encryptionType);
        var originalData = Encoding.UTF8.GetBytes("Top secret message! 🔒");

        // Act
        var encrypted = EncryptionUtils.Encrypt(originalData, key, encryptionType);
        var decrypted = EncryptionUtils.Decrypt(encrypted, key, encryptionType);

        // Assert
        Assert.That(decrypted, Is.EqualTo(originalData));
        Assert.That(Encoding.UTF8.GetString(decrypted), Is.EqualTo("Top secret message! 🔒"));
    }

    [Test]
    public void Decrypt_AES256_WithWrongKey_ShouldThrowException()
    {
        // Arrange
        var correctKey = EncryptionUtils.GenerateKey(EncryptionType.AES256);
        var wrongKey = EncryptionUtils.GenerateKey(EncryptionType.AES256);
        var encrypted = EncryptionUtils.Encrypt(TestData, correctKey, EncryptionType.AES256);

        // Act & Assert
        Assert.Throws<CryptographicException>(() =>
        {
            EncryptionUtils.Decrypt(encrypted, wrongKey, EncryptionType.AES256);
        });
    }

    [Test]
    public void Decrypt_ChaCha20_WithWrongKey_ShouldThrowException()
    {
        // Arrange
        var correctKey = EncryptionUtils.GenerateKey(EncryptionType.ChaCha20Poly1305);
        var wrongKey = EncryptionUtils.GenerateKey(EncryptionType.ChaCha20Poly1305);
        var encrypted = EncryptionUtils.Encrypt(TestData, correctKey, EncryptionType.ChaCha20Poly1305);

        // Act & Assert - ChaCha20 throws AuthenticationTagMismatchException, which is a subclass of CryptographicException
        Assert.Throws<System.Security.Cryptography.AuthenticationTagMismatchException>(() =>
        {
            EncryptionUtils.Decrypt(encrypted, wrongKey, EncryptionType.ChaCha20Poly1305);
        });
    }

    [Test]
    public void Encrypt_AES256_WithInvalidKeySize_ShouldThrowException()
    {
        // Arrange
        var invalidKey = new byte[16]; // Wrong size, should be 32

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            EncryptionUtils.Encrypt(TestData, invalidKey, EncryptionType.AES256);
        });
        Assert.That(ex.Message, Does.Contain("32-byte key"));
    }

    [Test]
    public void Encrypt_ChaCha20_WithInvalidKeySize_ShouldThrowException()
    {
        // Arrange
        var invalidKey = new byte[16]; // Wrong size, should be 32

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
        {
            EncryptionUtils.Encrypt(TestData, invalidKey, EncryptionType.ChaCha20Poly1305);
        });
        Assert.That(ex.Message, Does.Contain("32-byte key"));
    }

    [Test]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void Encrypt_WithEmptyData_ShouldHandleGracefully(EncryptionType encryptionType)
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(encryptionType);
        var emptyData = Array.Empty<byte>();

        // Act
        var encrypted = EncryptionUtils.Encrypt(emptyData, key, encryptionType);
        var decrypted = EncryptionUtils.Decrypt(encrypted, key, encryptionType);

        // Assert
        Assert.That(decrypted, Is.Empty);
    }

    [Test]
    [TestCase(EncryptionType.AES256)]
    [TestCase(EncryptionType.ChaCha20Poly1305)]
    public void Encrypt_WithLargeData_ShouldHandleCorrectly(EncryptionType encryptionType)
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(encryptionType);
        var largeData = new byte[1024 * 1024]; // 1MB
        Random.Shared.NextBytes(largeData);

        // Act
        var encrypted = EncryptionUtils.Encrypt(largeData, key, encryptionType);
        var decrypted = EncryptionUtils.Decrypt(encrypted, key, encryptionType);

        // Assert
        Assert.That(decrypted, Is.EqualTo(largeData));
    }

    [Test]
    public void Encrypt_AES256_ShouldIncludeIVInOutput()
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(EncryptionType.AES256);

        // Act
        var encrypted = EncryptionUtils.Encrypt(TestData, key, EncryptionType.AES256);

        // Assert - AES256 output should be at least IV (16 bytes) + some encrypted data
        Assert.That(encrypted.Length, Is.GreaterThan(16));
    }

    [Test]
    public void Encrypt_ChaCha20_ShouldIncludeNonceAndTagInOutput()
    {
        // Arrange
        var key = EncryptionUtils.GenerateKey(EncryptionType.ChaCha20Poly1305);

        // Act
        var encrypted = EncryptionUtils.Encrypt(TestData, key, EncryptionType.ChaCha20Poly1305);

        // Assert - ChaCha20 output should be at least nonce (12 bytes) + tag (16 bytes) + some encrypted data
        Assert.That(encrypted.Length, Is.GreaterThan(28));
    }
}
