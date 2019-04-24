namespace MinecraftClient.Protocol.Packets
{
    internal abstract class GamePacketHandler : IGamePacketHandler
    {
        protected abstract int MinVersion { get; }
        protected abstract int PacketId { get; }

        int IGamePacketHandler.MinVersion()
        {
            return MinVersion;
        }

        int IGamePacketHandler.PacketId()
        {
            return PacketId;
        }

        public abstract int PacketIntType();
    }
}