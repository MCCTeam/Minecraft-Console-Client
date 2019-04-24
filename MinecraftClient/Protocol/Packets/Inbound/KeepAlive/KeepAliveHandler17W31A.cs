using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.KeepAlive
{
    internal class KeepAliveHandler17W31A : KeepAliveHandler112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;

        protected override int PacketId => 0x20;
    }
}