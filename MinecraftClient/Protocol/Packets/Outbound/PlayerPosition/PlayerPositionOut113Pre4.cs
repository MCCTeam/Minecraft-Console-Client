using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut113Pre4 : PlayerPositionOut17W46A
    {
        protected override int MinVersion => PacketUtils.MC113pre4Version;
        protected override int PacketId => 0x0E;
    }
}