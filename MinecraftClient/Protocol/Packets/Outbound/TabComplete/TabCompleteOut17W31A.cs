using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.TabComplete
{
    internal class TabCompleteOut17W31A : TabCompleteOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x01;
    }
}