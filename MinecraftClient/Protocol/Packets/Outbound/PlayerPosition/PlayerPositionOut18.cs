using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut18 : PlayerPositionOut
    {
        protected override int MinVersion => PacketUtils.MC18Version;

        protected override byte[] GetExtraY(PlayerPositionRequest data)
        {
            return new byte[0];
        }
    }
}