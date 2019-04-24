using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChatMessage
{
    internal class ChatMessageHandler19 : ChatMessageHandler
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x0F;
    }
}