using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut113Pre7 : PlayerPositionOut113Pre4
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x10;
    }
}