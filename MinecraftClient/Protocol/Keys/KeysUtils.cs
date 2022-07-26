using System;

namespace MinecraftClient.Protocol.Keys
{
    public static class KeysUtils
    {
        private static string certificates = "https://api.minecraftservices.com/player/certificates";

        public static KeysInfo GetKeys(string accessToken)
        {

            var request = new ProxiedWebRequest(certificates);
            request.Accept = "application/json";
            request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));
            ConsoleIO.WriteLine("Token: " + accessToken);

            var response = request.Post("application/json", "");

            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLine(response.Body.ToString());
            }

            string jsonString = response.Body;
            Json.JSONData json = Json.ParseJson(jsonString);

            return new KeysInfo()
            {
                KeyPair = new KeyPair()
                {
                    PrivateKey = StripKeyHeaders(json.Properties["keyPair"].Properties["privateKey"].StringValue, "PRIVATE"),
                    PublicKey = StripKeyHeaders(json.Properties["keyPair"].Properties["publicKey"].StringValue, "PUBLIC"),
                },

                PublicKeySignature = json.Properties["publicKeySignature"].StringValue,
                PublicKeySignatureV2 = json.Properties["publicKeySignatureV2"].StringValue,
                ExpiresAt = json.Properties["expiresAt"].StringValue,
                RefreshedAfter = json.Properties["refreshedAfter"].StringValue,
            };
        }
        public static byte[] KeyFromBase64ToByteArray(string key)
        {
            return Convert.FromBase64String(key);
        }

        private static string StripKeyHeaders(string key, string type)
        {
            return key
                .Replace("-----BEGIN RSA " + type.ToUpper() + " KEY-----\n", "")
                .Replace("-----END RSA " + type.ToUpper() + " KEY-----\n", "")
                .Replace("\n", "")
                .Trim();
        }
    }
}
