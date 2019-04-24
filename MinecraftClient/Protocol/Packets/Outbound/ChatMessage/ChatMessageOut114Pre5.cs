using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ChatMessage
{
    internal class ChatMessageOut114Pre5 : ChatMessageOut113Pre7
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x03;
    }
}