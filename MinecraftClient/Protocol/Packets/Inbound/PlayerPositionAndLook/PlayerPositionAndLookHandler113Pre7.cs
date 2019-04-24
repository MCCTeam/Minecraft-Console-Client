using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookHandler113Pre7 : PlayerPositionAndLookHandler18W01A
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x32;
    }
}