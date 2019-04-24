using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut113Pre7 : KeepAliveOut113Pre4
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x0E;
    }
}