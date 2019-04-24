using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal class RespawnHandler17W45A : RespawnHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC17w45aVersion;
        protected override int PacketId => 0x36;
    }
}