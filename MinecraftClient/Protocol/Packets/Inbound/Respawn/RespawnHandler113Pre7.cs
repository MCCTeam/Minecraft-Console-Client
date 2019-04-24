using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal class RespawnHandler113Pre7 : RespawnHandler18W01A
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x38;
    }
}