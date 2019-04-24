using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookHandler112Pre5 : PlayerPositionAndLookHandler17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x2E;
    }
}