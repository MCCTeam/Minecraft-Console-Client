using System;


namespace MinecraftClient.Protocol
{
    [Serializable]
    public class SessionToken
    {
        public string ID { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string PlayerID { get; set; } = string.Empty;
        public string ClientID { get; set; } = string.Empty;
    }
}
