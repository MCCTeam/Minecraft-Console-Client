using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.KeepAlive
{
    internal class KeepAliveHandler114Pre5 : KeepAliveHandler18W01A
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;

        protected override int PacketId => 0x20;
    }
}