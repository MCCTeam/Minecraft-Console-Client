using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut113Pre4 : PlayerPositionAndLookOut17W46A
    {
        protected override int MinVersion => PacketUtils.MC113pre4Version;
        protected override int PacketId => 0x0F;
    }
}