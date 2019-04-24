namespace MinecraftClient.Protocol.Packets
{
    internal interface IGamePacketHandler
    {
        int MinVersion();

        int PacketId();

        int PacketIntType();
    }
}