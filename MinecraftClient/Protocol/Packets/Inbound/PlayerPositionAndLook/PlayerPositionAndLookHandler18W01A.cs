using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookHandler18W01A : PlayerPositionAndLookHandler17W45A
    {
        protected override int MinVersion => PacketUtils.MC18w01aVersion;
        protected override int PacketId => 0x31;
    }
}