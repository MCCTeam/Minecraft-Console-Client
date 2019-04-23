using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut17W31A : PlayerPositionOut112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x0D;
    }
}