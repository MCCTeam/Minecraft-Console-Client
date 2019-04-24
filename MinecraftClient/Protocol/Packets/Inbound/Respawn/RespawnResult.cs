namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal struct RespawnResult : IInboundData
    {
        public int Dimension;
        public byte GameMode;
    }
}