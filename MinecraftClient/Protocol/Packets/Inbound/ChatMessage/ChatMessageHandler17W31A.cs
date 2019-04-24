using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChatMessage
{
    internal class ChatMessageHandler17W31A : ChatMessageHandler112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x0E;
    }
}