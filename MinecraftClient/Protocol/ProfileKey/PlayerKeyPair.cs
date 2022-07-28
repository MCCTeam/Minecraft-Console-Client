using System;
using System.Collections.Generic;
using System.IO;

namespace MinecraftClient.Protocol.Keys
{
    public class PlayerKeyPair
    {
        public PublicKey PublicKey;

        public PrivateKey PrivateKey;
        public DateTime ExpiresAt { get; set; }
        public DateTime RefreshedAfter { get; set; }

        private const string DataTimeFormat = "O";

        public PlayerKeyPair(PublicKey keyPublic, PrivateKey keyPrivate, string expiresAt, string refreshedAfter)
        {
            PublicKey = keyPublic;
            PrivateKey = keyPrivate;
            ExpiresAt = DateTime.Parse(expiresAt).ToUniversalTime();
            RefreshedAfter = DateTime.Parse(refreshedAfter).ToUniversalTime();
        }

        public bool needRefresh()
        {
            return DateTime.Now.ToUniversalTime() > this.RefreshedAfter;
        }

        public bool isExpired()
        {
            return DateTime.Now.ToUniversalTime() > this.ExpiresAt;
        }

        public static PlayerKeyPair FromString(string tokenString)
        {
            string[] fields = tokenString.Split(',');

            if (fields.Length < 6)
                throw new InvalidDataException("Invalid string format");

            PublicKey publicKey = new PublicKey(pemKey: fields[0].Trim(),
                sig: fields[1].Trim(), sigV2: fields[2].Trim());

            PrivateKey privateKey = new PrivateKey(pemKey: fields[3].Trim());

            return new PlayerKeyPair(publicKey, privateKey, fields[4].Trim(), fields[5].Trim());
        }

        public override string ToString()
        {
            List<string> datas = new List<string>();
            datas.Add(Convert.ToBase64String(PublicKey.Key));
            datas.Add(Convert.ToBase64String(PublicKey.Signature));
            if (PublicKey.SignatureV2 == null)
                datas.Add(String.Empty);
            else
                datas.Add(Convert.ToBase64String(PublicKey.SignatureV2));
            datas.Add(Convert.ToBase64String(PrivateKey.Key));
            datas.Add(ExpiresAt.ToString(DataTimeFormat));
            datas.Add(RefreshedAfter.ToString(DataTimeFormat));
            return String.Join(",", datas.ToArray());
        }
    }
}
