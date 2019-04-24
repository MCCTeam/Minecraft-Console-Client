using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ChatMessage
{
    internal class ChatMessageOut17W13A : ChatMessageOut19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x03;
    }
}