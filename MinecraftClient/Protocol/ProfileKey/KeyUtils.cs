using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Message;
using static MinecraftClient.Protocol.Message.LastSeenMessageList;

namespace MinecraftClient.Protocol.ProfileKey
{
    static class KeyUtils
    {
        private static readonly SHA256 sha256Hash = SHA256.Create();

        /// <summary>
        /// Check whether the authentication server supports player profile keys.
        /// For Yggdrasil servers, this fetches the authlib-injector metadata and checks the
        /// <c>feature.enable_profile_key</c> flag documented at
        /// https://github.com/yushijinhun/authlib-injector/wiki/Yggdrasil-%E6%9C%8D%E5%8A%A1%E7%AB%AF%E6%8A%80%E6%9C%AF%E8%A7%84%E8%8C%83
        /// </summary>
        public static bool AuthServerSupportsProfileKeys(bool isYggdrasil)
        {
            if (!isYggdrasil)
                return true;

            ProxiedWebRequest.Response? response = null;
            try
            {
                var authServer = Settings.Config.Main.General.AuthServer;
                var request = new ProxiedWebRequest(
                    (authServer.UseHttps ? "https" : "http") + "://" + authServer.Host + ":" + authServer.Port + authServer.AuthlibInjectorAPIPath)
                {
                    Accept = "application/json"
                };

                response = request.Get();
                if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLine(response.Body.ToString());

                var json = Json.ParseJson(response.Body);
                bool enableProfileKey = json?["meta"]?["feature.enable_profile_key"]?.GetStringValue() == "true";
                return enableProfileKey;
            }
            catch (Exception e)
            {
                int code = response is null ? 0 : response.StatusCode;
                ConsoleIO.WriteLineFormatted("§cFetch authlib-injector metadata failed: HttpCode = " + code + ", Error = " + e.Message);
                if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
            }
            return false;
        }

        public static PlayerKeyPair? GetNewProfileKeys(string accessToken, bool isYggdrasil)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return null;

            if (!AuthServerSupportsProfileKeys(isYggdrasil))
            {
                if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLine("AuthServer does not support profile keys, will not attempt to fetch them.");
                return null;
            }

            string certificatesURL = "https://api.minecraftservices.com/player/certificates";
            if (isYggdrasil)
            {
                var authServer = Settings.Config.Main.General.AuthServer;
                certificatesURL = (authServer.UseHttps ? "https" : "http") + "://" + authServer.Host + ":" + authServer.Port +
                    authServer.AuthlibInjectorAPIPath + "/minecraftservices/player/certificates";
            }

            ProxiedWebRequest.Response? response = null;
            try
            {
                var request = new ProxiedWebRequest(certificatesURL)
                {
                    Accept = "application/json"
                };
                request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));

                response = request.Post("application/json", "");

                if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLine(response.Body.ToString());

                if (response.StatusCode < 200 || response.StatusCode >= 300)
                {
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(response.Body)
                        ? "Certificate endpoint returned an error response."
                        : response.Body);
                }

                var json = Json.ParseJson(response.Body);
                if (json?["keyPair"]?["publicKey"] is null
                    || json["keyPair"]?["privateKey"] is null
                    || json["publicKeySignature"] is null
                    || json["publicKeySignatureV2"] is null
                    || json["expiresAt"] is null
                    || json["refreshedAfter"] is null)
                {
                    throw new InvalidOperationException("Certificate endpoint returned an unexpected payload.");
                }

                PublicKey publicKey = new(pemKey: json!["keyPair"]!["publicKey"]!.GetStringValue(),
                    sig: json["publicKeySignature"]!.GetStringValue(),
                    sigV2: json["publicKeySignatureV2"]!.GetStringValue());

                PrivateKey privateKey = new(pemKey: json["keyPair"]!["privateKey"]!.GetStringValue());

                return new PlayerKeyPair(publicKey, privateKey,
                    expiresAt: json["expiresAt"]!.GetStringValue(),
                    refreshedAfter: json["refreshedAfter"]!.GetStringValue());
            }
            catch (Exception e)
            {
                int code = response is null ? 0 : response.StatusCode;
                ConsoleIO.WriteLineFormatted("§cFetch profile key failed: HttpCode = " + code + ", Error = " + e.Message);
                if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
                return null;
            }
        }

        public static byte[] DecodePemKey(string key, string prefix, string suffix)
        {
            int i = key.IndexOf(prefix);
            if (i != -1)
            {
                i += prefix.Length;
                int j = key.IndexOf(suffix, i);
                key = key[i..j];
            }
            key = key.Replace("\r", string.Empty);
            key = key.Replace("\n", string.Empty);
            return Convert.FromBase64String(key);
        }

        public static byte[] ComputeHash(byte[] data)
        {
            return sha256Hash.ComputeHash(data);
        }

        public static byte[] GetSignatureData(string message, Guid uuid, DateTimeOffset timestamp, ref byte[] salt)
        {
            List<byte> data = new();

            data.AddRange(salt);

            data.AddRange(uuid.ToBigEndianBytes());

            byte[] timestampByte = BitConverter.GetBytes(timestamp.ToUnixTimeSeconds());
            Array.Reverse(timestampByte);
            data.AddRange(timestampByte);

            data.AddRange(Encoding.UTF8.GetBytes(message));

            return data.ToArray();
        }

        public static byte[] GetSignatureData(string message, DateTimeOffset timestamp, ref byte[] salt, LastSeenMessageList lastSeenMessages)
        {
            List<byte> data = new();

            data.AddRange(salt);

            byte[] timestampByte = BitConverter.GetBytes(timestamp.ToUnixTimeSeconds());
            Array.Reverse(timestampByte);
            data.AddRange(timestampByte);

            data.AddRange(Encoding.UTF8.GetBytes(message));

            data.Add(70);

            lastSeenMessages.WriteForSign(data);

            return data.ToArray();
        }

        public static byte[] GetSignatureData_1_19_3(string message, Guid playerUuid, Guid chatUuid, int messageIndex, DateTimeOffset timestamp, ref byte[] salt, AcknowledgedMessage[] lastSeenMessages)
        {
            List<byte> data = new();

            // net.minecraft.network.message.SignedMessage#update
            data.AddRange(DataTypes.GetInt(1));

            // message link
            // net.minecraft.network.message.MessageLink#update
            data.AddRange(DataTypes.GetUUID(playerUuid));
            data.AddRange(DataTypes.GetUUID(chatUuid));
            data.AddRange(DataTypes.GetInt(messageIndex));

            // message body
            // net.minecraft.network.message.MessageBody#update
            data.AddRange(salt);
            data.AddRange(DataTypes.GetLong(timestamp.ToUnixTimeSeconds()));
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            data.AddRange(DataTypes.GetInt(messageBytes.Length));
            data.AddRange(messageBytes);
            data.AddRange(DataTypes.GetInt(lastSeenMessages.Length));
            foreach (AcknowledgedMessage ack in lastSeenMessages)
                data.AddRange(ack.signature);

            return data.ToArray();
        }

        public static byte[] GetSignatureData(byte[]? precedingSignature, Guid sender, byte[] bodySign)
        {
            List<byte> data = new();

            if (precedingSignature is not null)
                data.AddRange(precedingSignature);

            data.AddRange(sender.ToBigEndianBytes());

            data.AddRange(bodySign);

            return data.ToArray();
        }

        public static byte[] GetSignatureData(string message, DateTimeOffset timestamp, ref byte[] salt, int messageCount, Guid sender, Guid sessionUuid)
        {
            List<byte> data = new();

            // TODO!
            byte[] unknownInt1 = BitConverter.GetBytes(1);
            Array.Reverse(unknownInt1);
            data.AddRange(unknownInt1);

            data.AddRange(sender.ToBigEndianBytes());
            data.AddRange(sessionUuid.ToBigEndianBytes());

            byte[] msgCountByte = BitConverter.GetBytes(messageCount);
            Array.Reverse(msgCountByte);
            data.AddRange(msgCountByte);
            data.AddRange(salt);

            byte[] timestampByte = BitConverter.GetBytes(timestamp.ToUnixTimeSeconds());
            Array.Reverse(timestampByte);
            data.AddRange(timestampByte);

            byte[] msgByte = Encoding.UTF8.GetBytes(message);
            byte[] msgLengthByte = BitConverter.GetBytes(msgByte.Length);
            Array.Reverse(msgLengthByte);
            data.AddRange(msgLengthByte);
            data.AddRange(msgByte);

            byte[] unknownInt2 = BitConverter.GetBytes(0);
            Array.Reverse(unknownInt2);
            data.AddRange(unknownInt2);

            return data.ToArray();
        }

        // Delegate to the shared Json.EscapeString backed by System.Text.Json
        public static string EscapeString(string src) => Json.EscapeString(src);
    }
}
