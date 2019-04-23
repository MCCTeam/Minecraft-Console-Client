using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut17W46A : PlayerPositionOut17W45A
    {
        protected override int MinVersion => PacketUtils.MC17w46aVersion;
        protected override int PacketId => 0x0D;
    }
}