using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ChatMessage
{
    internal class ChatMessageOut17W31A : ChatMessageOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x02;
    }
}