using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol.Keys
{
    public class PublicKey
    {
        public byte[] Key { get; set; }
        public byte[]? Signature { get; set; }
        public byte[]? SignatureV2 { get; set; }

        private readonly RSA rsa;

        public PublicKey(string pemKey, string? sig = null, string? sigV2 = null)
        {
            this.Key = KeyUtils.DecodePemKey(pemKey, "-----BEGIN RSA PUBLIC KEY-----", "-----END RSA PUBLIC KEY-----");

            this.rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(this.Key, out _);

            if (!string.IsNullOrEmpty(sig))
                this.Signature = Convert.FromBase64String(sig);

            if (!string.IsNullOrEmpty(sigV2))
                this.SignatureV2 = Convert.FromBase64String(sigV2!);
        }

        public PublicKey(byte[] key, byte[] signature)
        {
            this.Key = key;

            this.rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(this.Key, out _);

            this.Signature = signature;
        }

        public bool VerifyData(byte[] data, byte[] signature)
        {
            return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        public bool VerifyMessage(string message, string uuid, DateTimeOffset timestamp, ref byte[] salt, ref byte[] signature)
        {
            byte[] data = KeyUtils.GetSignatureData(message, uuid, timestamp, ref salt);

            return VerifyData(data, signature);
        }

    }
}
