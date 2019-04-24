using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.KeepAlive
{
    internal class KeepAliveHandler19 : KeepAliveHandler
    {
        protected override int MinVersion => PacketUtils.MC19Version;

        protected override int PacketId => 0x1F;
    }
}