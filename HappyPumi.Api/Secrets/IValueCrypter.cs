#nullable enable

namespace HappyPumi.Api.Secrets;

/// <summary>
/// Encrypts and decrypts stack secret values for the service-managed secrets provider (the crypter
/// behind <c>/encrypt</c>, <c>/decrypt</c> and their batch variants). Round-trips: <c>Decrypt(Encrypt(x)) == x</c>.
/// </summary>
public interface IValueCrypter
{
    /// <summary>Encrypts <paramref name="plaintext"/>; the result is what the CLI stores in the checkpoint.</summary>
    byte[] Encrypt(byte[] plaintext);

    /// <summary>Decrypts a ciphertext produced by <see cref="Encrypt"/>.</summary>
    byte[] Decrypt(byte[] ciphertext);
}
