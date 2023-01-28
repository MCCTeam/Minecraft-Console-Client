using System;
using System.Linq;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.ProfileKey;

namespace MinecraftClient.Protocol
{
    public class PlayerInfo
    {
        public readonly Guid Uuid;

        public readonly string Name;

        // Tuple<Name, Value, Signature(empty if there is no signature)
        public readonly Tuple<string, string, string?>[]? Property;

        public int Gamemode;

        public int Ping;

        public string? DisplayName;

        public bool Listed = true;

        // Entity info

        public Mapping.Entity? entity;

        // For message signature

        public int MessageIndex = -1;

        public Guid ChatUuid = Guid.Empty;

        private PublicKey? PublicKey;

        private DateTime? KeyExpiresAt;

        private bool lastMessageVerified;

        private byte[]? precedingSignature;

        public PlayerInfo(Guid uuid, string name, Tuple<string, string, string?>[]? property, int gamemode, int ping, string? displayName, long? timeStamp, byte[]? publicKey, byte[]? signature)
        {
            Uuid = uuid;
            Name = name;
            if (property != null)
                Property = property;
            Gamemode = gamemode;
            Ping = ping;
            DisplayName = displayName;
            lastMessageVerified = false;
            if (timeStamp != null && publicKey != null && signature != null)
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)timeStamp);
                KeyExpiresAt = dateTimeOffset.UtcDateTime;
                try
                {
                    PublicKey = new PublicKey(publicKey, signature);
                    lastMessageVerified = true;
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    PublicKey = null;
                }
            }
            precedingSignature = null;
        }

        public PlayerInfo(string name, Guid uuid)
        {
            Name = name;
            Uuid = uuid;
            Gamemode = -1;
            Ping = 0;
            lastMessageVerified = true;
            precedingSignature = null;
        }

        public void ClearPublicKey()
        {
            ChatUuid = Guid.Empty;
            PublicKey = null;
            KeyExpiresAt = null;
        }

        public void SetPublicKey(Guid chatUuid, long publicKeyExpiryTime, byte[] encodedPublicKey, byte[] publicKeySignature)
        {
            ChatUuid = chatUuid;
            KeyExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(publicKeyExpiryTime).UtcDateTime;
            try
            {
                PublicKey = new PublicKey(encodedPublicKey, publicKeySignature);
                lastMessageVerified = true;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                PublicKey = null;
            }
        }

        public bool IsMessageChainLegal()
        {
            return lastMessageVerified;
        }

        public bool IsKeyExpired()
        {
            return DateTime.Now.ToUniversalTime() > KeyExpiresAt;
        }

        /// <summary>
        /// Verify message - 1.19
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <returns>Is this message vaild</returns>
        public bool VerifyMessage(string message, long timestamp, long salt, ref byte[] signature)
        {
            if (PublicKey == null || IsKeyExpired())
                return false;
            else
            {
                DateTimeOffset timeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);

                byte[] saltByte = BitConverter.GetBytes(salt);
                Array.Reverse(saltByte);

                return PublicKey.VerifyMessage(message, Uuid, timeOffset, ref saltByte, ref signature);
            }
        }

        /// <summary>
        /// Verify message - 1.19.1 and 1.19.2
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <param name="precedingSignature">Preceding message signature</param>
        /// <param name="lastSeenMessages">LastSeenMessages</param>
        /// <returns>Is this message chain vaild</returns>
        public bool VerifyMessage(string message, long timestamp, long salt, ref byte[] signature, ref byte[]? precedingSignature, LastSeenMessageList lastSeenMessages)
        {
            if (lastMessageVerified == false)
                return false;
            if (PublicKey == null || IsKeyExpired() || (this.precedingSignature != null && precedingSignature == null))
            {
                lastMessageVerified = false;
                return false;
            }
            if (this.precedingSignature != null && !this.precedingSignature.SequenceEqual(precedingSignature!))
            {
                lastMessageVerified = false;
                return false;
            }

            DateTimeOffset timeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);

            byte[] saltByte = BitConverter.GetBytes(salt);
            Array.Reverse(saltByte);

            bool res = PublicKey.VerifyMessage(message, Uuid, timeOffset, ref saltByte, ref signature, ref precedingSignature, lastSeenMessages);

            lastMessageVerified = res;
            this.precedingSignature = signature;

            return res;
        }

        /// <summary>
        /// Verify message head - 1.19.1 and 1.19.2
        /// </summary>
        /// <param name="precedingSignature">Preceding message signature</param>
        /// <param name="headerSignature">Message signature</param>
        /// <param name="bodyDigest">Message body hash</param>
        /// <returns>Is this message chain vaild</returns>
        public bool VerifyMessageHead(ref byte[]? precedingSignature, ref byte[] headerSignature, ref byte[] bodyDigest)
        {
            if (lastMessageVerified == false)
                return false;
            if (PublicKey == null || IsKeyExpired() || (this.precedingSignature != null && precedingSignature == null))
            {
                lastMessageVerified = false;
                return false;
            }
            if (this.precedingSignature != null && !this.precedingSignature.SequenceEqual(precedingSignature!))
            {
                lastMessageVerified = false;
                return false;
            }

            bool res = PublicKey.VerifyHeader(Uuid, ref bodyDigest, ref headerSignature, ref precedingSignature);

            lastMessageVerified = res;
            this.precedingSignature = headerSignature;

            return res;
        }

        /// <summary>
        /// Verify message - 1.19.3 and above
        /// </summary>
        /// <param name="message">Message content</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="salt">Salt</param>
        /// <param name="signature">Message signature</param>
        /// <param name="precedingSignature">Preceding message signature</param>
        /// <param name="lastSeenMessages">LastSeenMessages</param>
        /// <returns>Is this message chain vaild</returns>
        public bool VerifyMessage(string message, Guid playerUuid, Guid chatUuid, int messageIndex, long timestamp, long salt, ref byte[] signature, Tuple<int, byte[]?>[] previousMessageSignatures)
        {
            if (PublicKey == null || IsKeyExpired())
                return false;

            // net.minecraft.server.network.ServerPlayNetworkHandler#validateMessage
            return true;
        }
    }
}
