using System;
using System.Security.Cryptography;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.ProfileKey
{
    public class PublicKey
    {
        public byte[] Key { get; set; }
        public byte[]? Signature { get; set; }
        public byte[]? SignatureV2 { get; set; }

        private readonly RSA rsa;

        public PublicKey(string pemKey, string? sig = null, string? sigV2 = null)
        {
            Key = KeyUtils.DecodePemKey(pemKey, "-----BEGIN RSA PUBLIC KEY-----", "-----END RSA PUBLIC KEY-----");

            rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Key, out _);

            if (!string.IsNullOrEmpty(sig))
                Signature = Convert.FromBase64String(sig);

            if (!string.IsNullOrEmpty(sigV2))
                SignatureV2 = Convert.FromBase64String(sigV2!);

            if (SignatureV2 == null || SignatureV2.Length == 0)
                SignatureV2 = Signature;

            if (Signature == null || Signature.Length == 0)
                Signature = SignatureV2;
        }

        public PublicKey(byte[] key, byte[] signature)
        {
            Key = key;

            rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Key, out _);

            Signature = signature;
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
        /// Verify message - 1.19.1 and 1.19.2
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
        /// Verify message head - 1.19.1 and 1.19.2
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
