using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut17W45A : KeepAliveOut112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w45aVersion;
        protected override int PacketId => 0x0A;
    }
}