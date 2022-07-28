using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol.Keys
{
    public class KeyUtils
    {
        private static string certificates = "https://api.minecraftservices.com/player/certificates";

        public static PlayerKeyPair? GetKeys(string accessToken)
        {
            ProxiedWebRequest.Response? response = null;
            try
            {
                var request = new ProxiedWebRequest(certificates)
                {
                    Accept = "application/json"
                };
                request.Headers.Add("Authorization", string.Format("Bearer {0}", accessToken));

                response = request.Post("application/json", "");

                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLine(response.Body.ToString());
                }

                string jsonString = response.Body;
                Json.JSONData json = Json.ParseJson(jsonString);

                PublicKey publicKey = new(pemKey: json.Properties["keyPair"].Properties["publicKey"].StringValue,
                    sig: json.Properties["publicKeySignature"].StringValue,
                    sigV2: json.Properties["publicKeySignatureV2"].StringValue);

                PrivateKey privateKey = new(pemKey: json.Properties["keyPair"].Properties["privateKey"].StringValue);

                return new PlayerKeyPair(publicKey, privateKey,
                    expiresAt: json.Properties["expiresAt"].StringValue,
                    refreshedAfter: json.Properties["refreshedAfter"].StringValue);
            }
            catch (Exception e)
            {
                int code = (response == null) ? 0 : response.StatusCode;
                ConsoleIO.WriteLineFormatted("§cFetch profile key failed: HttpCode = " + code + ", Error = " + e.Message);
                if (Settings.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§c" + e.StackTrace);
                }
                return null;
            }
        }

        public static byte[] DecodePemKey(String key, String prefix, String suffix)
        {
            int i = key.IndexOf(prefix);
            if (i != -1)
            {
                i += prefix.Length;
                int j = key.IndexOf(suffix, i);
                key = key[i..j];
            }
            key = key.Replace("\r", String.Empty);
            key = key.Replace("\n", String.Empty);
            return Convert.FromBase64String(key);
        }

    }
}
