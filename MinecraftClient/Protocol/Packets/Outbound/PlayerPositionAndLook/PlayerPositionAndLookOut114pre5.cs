using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut114pre5 : PlayerPositionAndLookOut113Pre7
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x12;
    }
}