using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ChatMessage
{
    internal class ChatMessageOut19 : ChatMessageOut
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x02;
    }
}