using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChatMessage
{
    internal class ChatMessageHandler112Pre5 : ChatMessageHandler17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x0F;
    }
}