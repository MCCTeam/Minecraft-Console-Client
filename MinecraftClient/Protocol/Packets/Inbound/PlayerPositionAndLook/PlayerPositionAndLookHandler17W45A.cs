using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookHandler17W45A : PlayerPositionAndLookHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC17w45aVersion;
        protected override int PacketId => 0x30;
    }
}