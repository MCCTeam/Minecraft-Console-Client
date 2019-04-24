using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.TabComplete
{
    internal class TabCompleteOut17W45A : TabCompleteOut17W31A
    {
        protected override int MinVersion => PacketUtils.MC17w45aVersion;
        protected override int PacketId => -1;
    }
}