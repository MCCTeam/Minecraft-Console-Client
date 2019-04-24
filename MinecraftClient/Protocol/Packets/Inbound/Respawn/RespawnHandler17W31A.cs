using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal class RespawnHandler17W31A : RespawnHandler112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x35;
    }
}