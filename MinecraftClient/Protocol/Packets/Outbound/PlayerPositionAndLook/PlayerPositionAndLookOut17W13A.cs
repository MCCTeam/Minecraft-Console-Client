using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut17W13A : PlayerPositionAndLookOut19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x0E;
    }
}