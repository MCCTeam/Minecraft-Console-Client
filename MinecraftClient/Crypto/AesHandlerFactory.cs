using System;
using MinecraftClient.Crypto.AesHandler;

namespace MinecraftClient.Crypto;

internal static class AesHandlerFactory
{
    public static IAesHandler Create(ReadOnlySpan<byte> key)
    {
        byte[] ownedKey = key.ToArray();

        if (FasterAesX86.IsSupported())
            return new FasterAesX86(ownedKey);

        if (FasterAesArm.IsSupported())
            return new FasterAesArm(ownedKey);

        return new BasicAes(ownedKey);
    }
}
