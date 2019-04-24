using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal class RespawnHandler18W01A : RespawnHandler17W45A
    {
        protected override int MinVersion => PacketUtils.MC18w01aVersion;
        protected override int PacketId => 0x37;
    }
}