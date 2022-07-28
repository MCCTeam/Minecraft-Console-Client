using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol.Keys
{
    public class PrivateKey
    {
        public byte[] Key { get; set; }

        private readonly RSA rsa;

        public PrivateKey(string pemKey)
        {
            this.Key = KeyUtils.DecodePemKey(pemKey, "-----BEGIN RSA PRIVATE KEY-----", "-----END RSA PRIVATE KEY-----");

            this.rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(this.Key, out _);
        }

        public byte[] SignData(byte[] data)
        {
            return this.rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

    }
}
