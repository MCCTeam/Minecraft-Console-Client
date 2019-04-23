using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut17W46A : PlayerPositionAndLookOut17W45A
    {
        protected override int MinVersion => PacketUtils.MC17w46aVersion;
        protected override int PacketId => 0x0E;
    }
}