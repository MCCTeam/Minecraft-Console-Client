using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal class RespawnHandler19 : RespawnHandler
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x33;
    }
}