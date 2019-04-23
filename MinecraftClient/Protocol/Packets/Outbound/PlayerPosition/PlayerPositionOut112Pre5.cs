using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut112Pre5 : PlayerPositionOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x0E;
    }
}