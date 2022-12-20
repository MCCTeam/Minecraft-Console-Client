using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Crypto.AesHandler
{
    // https://github.com/Metalnem/aes-armv8
    public class FasterAesArm : IAesHandler
    {
        private const int BlockSize = 16;
        private const int Rounds = 10;

        private readonly byte[] enc;

        public FasterAesArm(Span<byte> key)
        {
            enc = new byte[(Rounds + 1) * BlockSize];

            int[] intKey = GenerateKeyExpansion(key);
            for (int i = 0; i < intKey.Length; ++i)
            {
                enc[i * 4 + 0] = (byte)((intKey[i] >> 0) & 0xff);
                enc[i * 4 + 1] = (byte)((intKey[i] >> 8) & 0xff);
                enc[i * 4 + 2] = (byte)((intKey[i] >> 16) & 0xff);
                enc[i * 4 + 3] = (byte)((intKey[i] >> 24) & 0xff);
            }
        }

        /// <summary>
        /// Detects if the required instruction set is supported
        /// </summary>
        /// <returns>Is it supported</returns>
        public static bool IsSupported()
        {
            return Aes.IsSupported && AdvSimd.IsSupported;
        }

        public override void EncryptEcb(Span<byte> plaintext, Span<byte> destination)
        {
            int position = 0;
            int left = plaintext.Length;

            var key0 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[0 * BlockSize]);
            var key1 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[1 * BlockSize]);
            var key2 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[2 * BlockSize]);
            var key3 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[3 * BlockSize]);
            var key4 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[4 * BlockSize]);
            var key5 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[5 * BlockSize]);
            var key6 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[6 * BlockSize]);
            var key7 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[7 * BlockSize]);
            var key8 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[8 * BlockSize]);
            var key9 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[9 * BlockSize]);
            var key10 = Unsafe.ReadUnaligned<Vector128<byte>>(ref enc[10 * BlockSize]);

            while (left >= BlockSize)
            {
                var block = Unsafe.ReadUnaligned<Vector128<byte>>(ref plaintext[position]);

                block = Aes.Encrypt(block, key0);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key1);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key2);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key3);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key4);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key5);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key6);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key7);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key8);
                block = Aes.MixColumns(block);

                block = Aes.Encrypt(block, key9);
                block = AdvSimd.Xor(block, key10);

                Unsafe.WriteUnaligned(ref destination[position], block);

                position += BlockSize;
                left -= BlockSize;
            }
        }

        private int[] GenerateKeyExpansion(Span<byte> rgbKey)
        {
            var m_encryptKeyExpansion = new int[4 * (Rounds + 1)];

            int index = 0;
            for (int i = 0; i < 4; ++i)
            {
                int i0 = rgbKey[index++];
                int i1 = rgbKey[index++];
                int i2 = rgbKey[index++];
                int i3 = rgbKey[index++];
                m_encryptKeyExpansion[i] = i3 << 24 | i2 << 16 | i1 << 8 | i0;
            }

            for (int i = 4; i < 4 * (Rounds + 1); ++i)
            {
                int iTemp = m_encryptKeyExpansion[i - 1];

                if (i % 4 == 0)
                {
                    iTemp = SubWord(Rot3(iTemp));
                    iTemp ^= s_Rcon[(i / 4) - 1];
                }

                m_encryptKeyExpansion[i] = m_encryptKeyExpansion[i - 4] ^ iTemp;
            }

            return m_encryptKeyExpansion;
        }

        private static int SubWord(int a)
        {
            return s_Sbox[a & 0xFF] |
                   s_Sbox[a >> 8 & 0xFF] << 8 |
                   s_Sbox[a >> 16 & 0xFF] << 16 |
                   s_Sbox[a >> 24 & 0xFF] << 24;
        }

        private static int Rot3(int val)
        {
            return (val << 24 & unchecked((int)0xFF000000)) | (val >> 8 & unchecked((int)0x00FFFFFF));
        }

        private static readonly byte[] s_Sbox = new byte[] {
             99, 124, 119, 123, 242, 107, 111, 197,  48,   1, 103,  43, 254, 215, 171, 118,
            202, 130, 201, 125, 250,  89,  71, 240, 173, 212, 162, 175, 156, 164, 114, 192,
            183, 253, 147,  38,  54,  63, 247, 204,  52, 165, 229, 241, 113, 216,  49,  21,
              4, 199,  35, 195,  24, 150,   5, 154,   7,  18, 128, 226, 235,  39, 178, 117,
              9, 131,  44,  26,  27, 110,  90, 160,  82,  59, 214, 179,  41, 227,  47, 132,
             83, 209,   0, 237,  32, 252, 177,  91, 106, 203, 190,  57,  74,  76,  88, 207,
            208, 239, 170, 251,  67,  77,  51, 133,  69, 249,   2, 127,  80,  60, 159, 168,
             81, 163,  64, 143, 146, 157,  56, 245, 188, 182, 218,  33,  16, 255, 243, 210,
            205,  12,  19, 236,  95, 151,  68,  23, 196, 167, 126,  61, 100,  93,  25, 115,
             96, 129,  79, 220,  34,  42, 144, 136,  70, 238, 184,  20, 222,  94,  11, 219,
            224,  50,  58,  10,  73,   6,  36,  92, 194, 211, 172,  98, 145, 149, 228, 121,
            231, 200,  55, 109, 141, 213,  78, 169, 108,  86, 244, 234, 101, 122, 174,   8,
            186, 120,  37,  46,  28, 166, 180, 198, 232, 221, 116,  31,  75, 189, 139, 138,
            112,  62, 181, 102,  72,   3, 246,  14,  97,  53,  87, 185, 134, 193,  29, 158,
            225, 248, 152,  17, 105, 217, 142, 148, 155,  30, 135, 233, 206,  85,  40, 223,
            140, 161, 137,  13, 191, 230,  66, 104,  65, 153,  45,  15, 176,  84, 187,  22 };

        private static readonly int[] s_Rcon = new int[] {
            0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36,
            0x6c, 0xd8, 0xab, 0x4d, 0x9a, 0x2f, 0x5e, 0xbc, 0x63, 0xc6,
            0x97, 0x35, 0x6a, 0xd4, 0xb3, 0x7d, 0xfa, 0xef, 0xc5, 0x91 };
    }
}
