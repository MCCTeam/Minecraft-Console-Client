using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.KeepAlive
{
    internal class KeepAliveHandler17W13A : KeepAliveHandler19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;

        protected override int PacketId => 0x20;
    }
}