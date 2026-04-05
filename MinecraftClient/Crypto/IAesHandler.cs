using System;

namespace MinecraftClient.Crypto;

public abstract class IAesHandler : IDisposable
{
    public abstract void EncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination);

    public virtual void Dispose()
    {
    }
}
