using System.IO;

namespace MinecraftClient.Protocol.Keys
{
    public class KeysInfo
    {
        public KeyPair KeyPair { get; set; }
        public string PublicKeySignature { get; set; }
        public string PublicKeySignatureV2 { get; set; }
        public string ExpiresAt { get; set; }
        public string RefreshedAfter { get; set; }

        public static KeysInfo FromString(string tokenString)
        {
            string[] fields = tokenString.Split(',');

            if (fields.Length < 5)
                throw new InvalidDataException("Invalid string format");

            KeysInfo key = new KeysInfo();
            key.KeyPair = new KeyPair
            {
                PrivateKey = fields[0].Trim(),
                PublicKey = fields[1].Trim()
            };

            key.PublicKeySignature = fields[2].Trim();
            key.PublicKeySignatureV2 = fields[3].Trim();
            key.ExpiresAt = fields[4].Trim();
            key.RefreshedAfter = fields[5].Trim();

            return key;
        }

        public override string ToString()
        {
            return KeyPair.PrivateKey + "," + KeyPair.PublicKey + "," + PublicKeySignature + "," + PublicKeySignature + "," + ExpiresAt + "," + RefreshedAfter;
        }
    }
}
