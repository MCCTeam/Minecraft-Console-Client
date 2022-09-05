using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Message;

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

        /// <summary>
        /// Verify message - 1.19
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="uuid">Sender uuid</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <returns>Is this message vaild</returns>
        public bool VerifyMessage(string message, Guid uuid, DateTimeOffset timestamp, ref byte[] salt, ref byte[] signature)
        {
            byte[] data = KeyUtils.GetSignatureData(message, uuid, timestamp, ref salt);

            return VerifyData(data, signature);
        }

        /// <summary>
        /// Verify message - 1.19.1 and above
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="uuid">Sender uuid</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <param name="precedingSignature">Preceding message signature</param>
        /// <param name="lastSeenMessages">LastSeenMessages</param>
        /// <returns>Is this message vaild</returns>
        public bool VerifyMessage(string message, Guid uuid, DateTimeOffset timestamp, ref byte[] salt, ref byte[] signature, ref byte[]? precedingSignature, LastSeenMessageList lastSeenMessages)
        {
            byte[] bodySignData = KeyUtils.GetSignatureData(message, timestamp, ref salt, lastSeenMessages);
            byte[] bodyDigest = KeyUtils.ComputeHash(bodySignData);

            byte[] msgSignData = KeyUtils.GetSignatureData(precedingSignature, uuid, bodyDigest);

            return VerifyData(msgSignData, signature);
        }

        /// <summary>
        /// Verify message head - 1.19.1 and above
        /// </summary>
        /// <param name="bodyDigest">Message body hash</param>
        /// <param name="signature">Message signature</param>
        /// <returns>Is this message header vaild</returns>
        public bool VerifyHeader(Guid uuid, ref byte[] bodyDigest, ref byte[] signature, ref byte[]? precedingSignature)
        {

            byte[] msgSignData = KeyUtils.GetSignatureData(precedingSignature, uuid, bodyDigest);

            return VerifyData(msgSignData, signature);
        }

    }
}
