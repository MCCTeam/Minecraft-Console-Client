using System;
using System.Text;

namespace MinecraftClient.Protocol
{
    // Thanks to https://stackoverflow.com/questions/60404612/parse-jwt-token-to-get-the-payload-content-only-without-external-library-in-c-sh
    public static class JwtPayloadDecode
    {
        public static string GetPayload(string token)
        {
            var content = token.Split('.')[1];
            var jsonPayload = Encoding.UTF8.GetString(Decode(content));
            return jsonPayload;
        }

        private static byte[] Decode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new System.ArgumentOutOfRangeException("input", "Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }
}
