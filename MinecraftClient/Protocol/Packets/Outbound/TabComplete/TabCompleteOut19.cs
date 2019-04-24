using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.TabComplete
{
    internal class TabCompleteOut19 : TabCompleteOut
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x01;
    }
}