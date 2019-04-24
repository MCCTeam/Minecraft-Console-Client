using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut113Pre4 : KeepAliveOut17W46A
    {
        protected override int MinVersion => PacketUtils.MC113pre4Version;
        protected override int PacketId => 0x0C;
    }
}