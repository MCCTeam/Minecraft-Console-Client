using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut112Pre5 : PlayerPositionAndLookOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x0F;
    }
}