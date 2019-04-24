using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut112Pre5 : KeepAliveOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x0B;
    }
}