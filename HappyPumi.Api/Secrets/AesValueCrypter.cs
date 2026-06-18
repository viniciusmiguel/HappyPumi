#nullable enable

using System;
using System.Security.Cryptography;

namespace HappyPumi.Api.Secrets;

/// <summary>
/// AES-256-GCM <see cref="IValueCrypter"/>. The ciphertext layout is <c>nonce(12) || tag(16) || data</c>,
/// base64-encoded by the JSON layer.
/// </summary>
/// <remarks>
/// The data key is process-static (generated once per run). That matches the in-memory store (ADR-0005):
/// state — including the secrets encrypted into the checkpoint — is lost on restart anyway. A durable
/// deployment must derive a persisted, per-stack key (an ADR-0007 secrets follow-up) before this is more
/// than a dev secrets provider.
/// </remarks>
public sealed class AesValueCrypter : IValueCrypter
{
    private const int NonceSize = 12; // AES-GCM standard nonce length
    private const int TagSize = 16;   // AES-GCM authentication tag length

    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32); // AES-256

    public byte[] Encrypt(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var tag = new byte[TagSize];
        var cipher = new byte[plaintext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        var result = new byte[NonceSize + TagSize + cipher.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipher, 0, result, NonceSize + TagSize, cipher.Length);
        return result;
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length < NonceSize + TagSize)
            throw new ArgumentException(
                $"Ciphertext is {ciphertext.Length} bytes; expected at least {NonceSize + TagSize} " +
                "(nonce + tag).", nameof(ciphertext));

        var nonce = ciphertext.AsSpan(0, NonceSize);
        var tag = ciphertext.AsSpan(NonceSize, TagSize);
        var cipher = ciphertext.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plaintext);
        return plaintext;
    }
}
