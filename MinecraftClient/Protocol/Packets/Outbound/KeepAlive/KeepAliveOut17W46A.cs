using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut17W46A : KeepAliveOut17W45A
    {
        protected override int MinVersion => PacketUtils.MC17w46aVersion;
        protected override int PacketId => 0x0B;
    }
}