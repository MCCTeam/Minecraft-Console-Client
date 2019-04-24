using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChunkData
{
    internal class ChunkDataHandler17W31A : ChunkDataHandler112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x21;
    }
}