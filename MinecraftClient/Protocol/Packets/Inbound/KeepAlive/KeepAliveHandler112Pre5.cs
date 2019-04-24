using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.KeepAlive
{
    internal class KeepAliveHandler112Pre5 : KeepAliveHandler17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;

        protected override int PacketId => 0x1F;
    }
}