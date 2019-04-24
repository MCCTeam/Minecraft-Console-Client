namespace MinecraftClient.Protocol.Packets.Inbound.ChatMessage
{
    internal struct ChatMessageResult: IInboundData
    {
        public byte MessageType { get; internal set; }
        public string Message { get; internal set; }
    }
}