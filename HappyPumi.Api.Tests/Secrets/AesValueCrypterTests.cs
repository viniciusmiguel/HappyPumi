using System.Text;
using HappyPumi.Api.Secrets;

namespace HappyPumi.Api.Tests.Secrets;

/// <summary>Unit tests for the AES-GCM value crypter behind the stack /encrypt and /decrypt endpoints.</summary>
public sealed class AesValueCrypterTests
{
    [Fact]
    public void EncryptThenDecryptRoundTrips()
    {
        var crypter = new AesValueCrypter();
        var plaintext = Encoding.UTF8.GetBytes("hunter2");

        var roundTripped = crypter.Decrypt(crypter.Encrypt(plaintext));

        Assert.Equal(plaintext, roundTripped);
    }

    [Fact]
    public void CiphertextDiffersFromPlaintextAndIsNonDeterministic()
    {
        var crypter = new AesValueCrypter();
        var plaintext = Encoding.UTF8.GetBytes("hunter2");

        var first = crypter.Encrypt(plaintext);
        var second = crypter.Encrypt(plaintext);

        Assert.NotEqual(plaintext, first);
        Assert.NotEqual(first, second); // random nonce per call
    }

    [Fact]
    public void TamperedCiphertextFailsAuthentication()
    {
        var crypter = new AesValueCrypter();
        var cipher = crypter.Encrypt(Encoding.UTF8.GetBytes("hunter2"));
        cipher[^1] ^= 0xFF;

        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => crypter.Decrypt(cipher));
    }

    [Fact]
    public void ShortCiphertextIsRejectedWithItsLength()
    {
        var crypter = new AesValueCrypter();

        var ex = Assert.Throws<ArgumentException>(() => crypter.Decrypt(new byte[4]));
        Assert.Contains("4 bytes", ex.Message);
    }
}
