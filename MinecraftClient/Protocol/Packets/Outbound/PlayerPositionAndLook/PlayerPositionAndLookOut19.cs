using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut19 : PlayerPositionAndLookOut18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x0D;
    }
}