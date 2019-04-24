namespace MinecraftClient.Protocol.Packets.Outbound.ClientSettings
{
    internal struct ClientSettingsRequest : IOutboundRequest
    {
        public string Language { get; set; }
        public byte ViewDistance { get; set; }
        public byte Difficulty { get; set; }
        public byte ChatMode { get; set; }
        public bool ChatColors { get; set; }
        public byte SkinParts { get; set; }
        public byte MainHand { get; set; }
    }
}