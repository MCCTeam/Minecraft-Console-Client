using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal class RespawnHandler112Pre5 : RespawnHandler17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x34;
    }
}