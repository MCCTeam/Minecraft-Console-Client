using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace MinecraftClient.Protocol.ProfileKey
{
    public class PlayerKeyPair
    {
        [JsonInclude]
        [JsonPropertyName("PublicKey")]
        public PublicKey PublicKey;

        [JsonInclude]
        [JsonPropertyName("PrivateKey")]
        public PrivateKey PrivateKey;

        [JsonInclude]
        [JsonPropertyName("ExpiresAt")]
        public DateTime ExpiresAt;

        [JsonInclude]
        [JsonPropertyName("RefreshedAfter")]
        public DateTime RefreshedAfter;

        [JsonIgnore]
        private const string DataTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        [JsonConstructor]
        public PlayerKeyPair(PublicKey PublicKey, PrivateKey PrivateKey, DateTime ExpiresAt, DateTime RefreshedAfter)
        {
            this.PublicKey = PublicKey;
            this.PrivateKey = PrivateKey;
            this.ExpiresAt = ExpiresAt;
            this.RefreshedAfter = RefreshedAfter;
        }

        public bool NeedRefresh()
        {
            return DateTime.Now.ToUniversalTime() > RefreshedAfter;
        }

        public bool IsExpired()
        {
            return DateTime.Now.ToUniversalTime() > ExpiresAt;
        }

        public long GetExpirationMilliseconds()
        {
            DateTimeOffset timeOffset = new(ExpiresAt);
            return timeOffset.ToUnixTimeMilliseconds();
        }

        public long GetExpirationSeconds()
        {
            DateTimeOffset timeOffset = new(ExpiresAt);
            return timeOffset.ToUnixTimeSeconds();
        }

        public override string ToString()
        {
            List<string> datas = new();
            datas.Add(Convert.ToBase64String(PublicKey.Key));
            if (PublicKey.Signature == null)
                datas.Add(string.Empty);
            else
                datas.Add(Convert.ToBase64String(PublicKey.Signature));
            if (PublicKey.SignatureV2 == null)
                datas.Add(string.Empty);
            else
                datas.Add(Convert.ToBase64String(PublicKey.SignatureV2));
            datas.Add(Convert.ToBase64String(PrivateKey.Key));
            datas.Add(ExpiresAt.ToString(DataTimeFormat));
            datas.Add(RefreshedAfter.ToString(DataTimeFormat));
            return string.Join(",", datas.ToArray());
        }
    }
}
