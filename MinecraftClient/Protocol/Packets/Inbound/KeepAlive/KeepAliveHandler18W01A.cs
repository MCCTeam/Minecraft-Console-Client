using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.KeepAlive
{
    internal class KeepAliveHandler18W01A : KeepAliveHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC18w01aVersion;

        protected override int PacketId => 0x21;
    }
}