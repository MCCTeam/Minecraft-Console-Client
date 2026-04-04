using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MinecraftClient.Crypto.AesHandler;

public sealed class FasterAesX86 : IAesHandler
{
    private Vector128<byte>[] RoundKeys { get; }

    public FasterAesX86(ReadOnlySpan<byte> key)
    {
        RoundKeys = KeyExpansion(key);
    }

    public static bool IsSupported()
    {
        return Sse2.IsSupported && Aes.IsSupported;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public override void EncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination)
    {
        Vector128<byte>[] keys = RoundKeys;

        ReadOnlySpan<Vector128<byte>> blocks = MemoryMarshal.Cast<byte, Vector128<byte>>(plaintext);
        Span<Vector128<byte>> dest = MemoryMarshal.Cast<byte, Vector128<byte>>(destination);

        _ = keys[10];

        for (int i = 0; i < blocks.Length; i++)
        {
            Vector128<byte> b = blocks[i];

            b = Sse2.Xor(b, keys[0]);
            b = Aes.Encrypt(b, keys[1]);
            b = Aes.Encrypt(b, keys[2]);
            b = Aes.Encrypt(b, keys[3]);
            b = Aes.Encrypt(b, keys[4]);
            b = Aes.Encrypt(b, keys[5]);
            b = Aes.Encrypt(b, keys[6]);
            b = Aes.Encrypt(b, keys[7]);
            b = Aes.Encrypt(b, keys[8]);
            b = Aes.Encrypt(b, keys[9]);
            b = Aes.EncryptLast(b, keys[10]);

            dest[i] = b;
        }
    }

    private static Vector128<byte>[] KeyExpansion(ReadOnlySpan<byte> key)
    {
        Vector128<byte>[] keys = new Vector128<byte>[20];

        keys[0] = Unsafe.ReadUnaligned<Vector128<byte>>(ref MemoryMarshal.GetReference(key));

        MakeRoundKey(keys, 1, 0x01);
        MakeRoundKey(keys, 2, 0x02);
        MakeRoundKey(keys, 3, 0x04);
        MakeRoundKey(keys, 4, 0x08);
        MakeRoundKey(keys, 5, 0x10);
        MakeRoundKey(keys, 6, 0x20);
        MakeRoundKey(keys, 7, 0x40);
        MakeRoundKey(keys, 8, 0x80);
        MakeRoundKey(keys, 9, 0x1B);
        MakeRoundKey(keys, 10, 0x36);

        for (int i = 1; i < 10; i++)
            keys[10 + i] = Aes.InverseMixColumns(keys[i]);

        return keys;
    }

    private static void MakeRoundKey(Vector128<byte>[] keys, int index, byte rcon)
    {
        Vector128<byte> s = keys[index - 1];
        Vector128<byte> t = keys[index - 1];

        t = Aes.KeygenAssist(t, rcon);
        t = Sse2.Shuffle(t.AsUInt32(), 0xFF).AsByte();

        s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 4));
        s = Sse2.Xor(s, Sse2.ShiftLeftLogical128BitLane(s, 8));

        keys[index] = Sse2.Xor(s, t);
    }
}
