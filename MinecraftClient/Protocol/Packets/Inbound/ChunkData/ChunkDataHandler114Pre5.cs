using System.Collections.Generic;
using System.IO;
using fNbt;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChunkData
{
    internal class ChunkDataHandler114Pre5 : ChunkDataHandler17W46A
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x21;

        protected override byte[] ReadHeightMap(List<byte> packetData)
        {
            var cp = packetData.ToArray();
            var nbt = new NbtFile();
            var read = nbt.LoadFromStream(new MemoryStream(cp), NbtCompression.AutoDetect);
            return PacketUtils.readData((int)read, packetData);
        }
    }
}