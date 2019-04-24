using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.TabComplete
{
    internal class TabCompleteOut17W46A : TabCompleteOut17W45A
    {
        protected override int MinVersion => PacketUtils.MC17w46aVersion;
        protected override int PacketId => 0x04;
    }
}