using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.ProfileKey
{
    static class KeyUtils
    {
        private static readonly SHA256 sha256Hash = SHA256.Create();

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

        public static byte[] GetSignatureData(byte[]? precedingSignature, Guid sender, byte[] bodySign)
        {
            List<byte> data = new();

            if (precedingSignature != null)
                data.AddRange(precedingSignature);

            data.AddRange(sender.ToBigEndianBytes());

            data.AddRange(bodySign);

            return data.ToArray();
        }

        // https://github.com/mono/mono/blob/master/mcs/class/System.Json/System.Json/JsonValue.cs
        public static string EscapeString(string src)
        {
            StringBuilder sb = new();

            int start = 0;
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                bool needEscape = c < 32 || c == '"' || c == '\\';
                // Broken lead surrogate
                needEscape = needEscape || c >= '\uD800' && c <= '\uDBFF' &&
                    (i == src.Length - 1 || src[i + 1] < '\uDC00' || src[i + 1] > '\uDFFF');
                // Broken tail surrogate
                needEscape = needEscape || c >= '\uDC00' && c <= '\uDFFF' &&
                    (i == 0 || src[i - 1] < '\uD800' || src[i - 1] > '\uDBFF');
                // To produce valid JavaScript
                needEscape = needEscape || c == '\u2028' || c == '\u2029';

                if (needEscape)
                {
                    sb.Append(src, start, i - start);
                    switch (src[i])
                    {
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        default:
                            sb.Append("\\u");
                            sb.Append(((int)src[i]).ToString("x04"));
                            break;
                    }
                    start = i + 1;
                }

            }
            sb.Append(src, start, src.Length - start);
            return sb.ToString();
        }
    }
}
