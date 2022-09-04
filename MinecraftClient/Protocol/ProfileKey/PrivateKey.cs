using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Keys
{
    public class PrivateKey
    {
        public byte[] Key { get; set; }

        private readonly RSA rsa;

        private byte[]? precedingSignature = null;

        public PrivateKey(string pemKey)
        {
            this.Key = KeyUtils.DecodePemKey(pemKey, "-----BEGIN RSA PRIVATE KEY-----", "-----END RSA PRIVATE KEY-----");

            this.rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(this.Key, out _);
        }

        public byte[] SignData(byte[] data)
        {
            return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        /// <summary>
        /// Sign message - 1.19
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="uuid">Sender uuid</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <returns>Signature data</returns>
        public byte[] SignMessage(string message, Guid uuid, DateTimeOffset timestamp, ref byte[] salt)
        {
            string messageJson = "{\"text\":\"" + KeyUtils.EscapeString(message) + "\"}";

            byte[] data = KeyUtils.GetSignatureData(messageJson, uuid, timestamp, ref salt);

            return SignData(data);
        }

        /// <summary>
        /// Sign message - 1.19.1 and above
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="uuid">Sender uuid</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="lastSeenMessages">LastSeenMessageList</param>
        /// <returns>Signature data</returns>
        public byte[] SignMessage(string message, Guid uuid, DateTimeOffset timestamp, ref byte[] salt, LastSeenMessageList lastSeenMessages)
        {
            byte[] bodySignData = KeyUtils.GetSignatureData(message, timestamp, ref salt, lastSeenMessages);
            byte[] bodyDigest = KeyUtils.ComputeHash(bodySignData);

            byte[] msgSignData = KeyUtils.GetSignatureData(precedingSignature, uuid, bodyDigest);
            byte[] msgSign = SignData(msgSignData);
            
            this.precedingSignature = msgSign;

            return msgSign;
        }

    }
}
