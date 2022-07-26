using System;

namespace MinecraftClient.Protocol.Keys
{
    [Serializable]
    public class KeyPair
    {
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
    }
}
