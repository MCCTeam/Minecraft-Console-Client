namespace MinecraftClient.Protocol.Packets.Outbound.PluginMessage
{
    internal struct PluginMessageRequest : IOutboundRequest
    {
        public string Channel { get; set; }
    }
}