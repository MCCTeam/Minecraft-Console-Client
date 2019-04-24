using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookHandler114Pre5 : PlayerPositionAndLookHandler113Pre7
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x35;
    }
}