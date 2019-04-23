using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut17W13A : PlayerPositionOut19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x0D;
    }
}