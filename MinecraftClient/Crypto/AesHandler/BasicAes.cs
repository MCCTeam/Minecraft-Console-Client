using System;
using System.Security.Cryptography;

namespace MinecraftClient.Crypto.AesHandler;

public sealed class BasicAes : IAesHandler
{
    private readonly Aes aes;

    public BasicAes(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        aes = Aes.Create();
        aes.BlockSize = 128;
        aes.KeySize = 128;
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
    }

    public override void EncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination)
    {
        aes.EncryptEcb(plaintext, destination, PaddingMode.None);
    }

    public override void Dispose()
    {
        aes.Dispose();
    }
}
