using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut17W13A : KeepAliveOut19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x0C;
    }
}