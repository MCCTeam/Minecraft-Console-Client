using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut19 : PlayerPositionOut18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x0C;
    }
}