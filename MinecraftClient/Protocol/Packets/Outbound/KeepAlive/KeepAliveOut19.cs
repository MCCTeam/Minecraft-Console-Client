using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut19 : KeepAliveOut
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x0B;
    }
}