using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChunkData
{
    internal class ChunkDataHandler114Pre5 : ChunkDataHandler17W46A
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x21;

        protected override byte[] ReadHeightMap(List<byte> packetData)
        {
            // NBT data stored in the array, 256 entities, 9 bits each, not sure how to read tho.
            return PacketUtils.readData(288, packetData);
        }
    }
}