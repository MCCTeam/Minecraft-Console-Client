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

        public byte[] SignMessage(string message, string uuid, DateTimeOffset timestamp, ref byte[] salt)
        {
            List<byte> data = new List<byte>();

            data.AddRange(salt);

            byte[] UUIDLeastSignificantBits = BitConverter.GetBytes(Convert.ToInt64(uuid[..16], 16));
            Array.Reverse(UUIDLeastSignificantBits);
            data.AddRange(UUIDLeastSignificantBits);

            byte[] UUIDMostSignificantBits = BitConverter.GetBytes(Convert.ToInt64(uuid.Substring(16, 16), 16));
            Array.Reverse(UUIDMostSignificantBits);
            data.AddRange(UUIDMostSignificantBits);

            byte[] timestampByte = BitConverter.GetBytes(timestamp.ToUnixTimeSeconds());
            Array.Reverse(timestampByte);
            data.AddRange(timestampByte);

            string messageJson = "{\"text\":\"" + KeyUtils.EscapeString(message) + "\"}";
            data.AddRange(Encoding.UTF8.GetBytes(messageJson));

            return SignData(data.ToArray());
        }

    }
}
