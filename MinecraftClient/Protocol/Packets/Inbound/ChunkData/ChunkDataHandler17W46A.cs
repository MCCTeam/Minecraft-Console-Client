using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChunkData
{
    internal class ChunkDataHandler17W46A : ChunkDataHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC17w46aVersion;
        protected override int PacketId => 0x22;
    }
}