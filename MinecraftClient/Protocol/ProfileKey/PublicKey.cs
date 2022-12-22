﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.ProfileKey
{
    public class PublicKey
    {
        [JsonInclude]
        [JsonPropertyName("Key")]
        public byte[] Key { get; set; }

        [JsonInclude]
        [JsonPropertyName("Signature")]
        public byte[]? Signature { get; set; }

        [JsonInclude]
        [JsonPropertyName("SignatureV2")]
        public byte[]? SignatureV2 { get; set; }

        [JsonIgnore]
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
        }

        public PublicKey(byte[] key, byte[] signature)
        {
            Key = key;

            rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Key, out _);

            Signature = signature;
        }

        [JsonConstructor]
        public PublicKey(byte[] Key, byte[]? Signature, byte[]? SignatureV2) : this(Key, Signature!)
        {
            this.SignatureV2 = SignatureV2;
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
