using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.TabComplete
{
    internal class TabCompleteOut113Pre7 : TabCompleteOut17W46A
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x05;
    }
}