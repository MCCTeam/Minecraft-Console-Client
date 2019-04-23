using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut18 : PlayerPositionAndLookOut
    {
        protected override int MinVersion => PacketUtils.MC18Version;

        protected override byte[] GetExtraY(PlayerPositionAndLookRequest data)
        {
            return new byte[0];
        }
    }
}