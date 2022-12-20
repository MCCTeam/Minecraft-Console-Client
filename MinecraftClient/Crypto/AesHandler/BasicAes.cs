using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Crypto.AesHandler
{
    public class BasicAes : IAesHandler
    {
        private readonly Aes Aes;

        public BasicAes(byte[] key)
        {
            Aes = Aes.Create();
            Aes.BlockSize = 128;
            Aes.KeySize = 128;
            Aes.Key = key;
            Aes.Mode = CipherMode.ECB;
            Aes.Padding = PaddingMode.None;
        }

        public override void EncryptEcb(Span<byte> plaintext, Span<byte> destination)
        {
            Aes.EncryptEcb(plaintext, destination, PaddingMode.None);
        }
    }
}
