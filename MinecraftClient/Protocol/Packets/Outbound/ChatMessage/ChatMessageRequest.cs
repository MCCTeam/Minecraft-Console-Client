namespace MinecraftClient.Protocol.Packets.Outbound.ChatMessage
{
    internal struct ChatMessageRequest : IOutboundRequest
    {
        public string Message { get; set; }
    }
}