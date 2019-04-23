using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChunkData
{
    internal class ChunkDataHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x21;
        protected override InboundTypes PackageType => InboundTypes.ChunkData;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            if (!Settings.TerrainAndMovements)
            {
                return null;
            }

            var chunkX = PacketUtils.readNextInt(packetData);
            var chunkZ = PacketUtils.readNextInt(packetData);
            var chunksContinuous = PacketUtils.readNextBool(packetData);

            var chunkMask = ReadChunkMask(packetData);
            ReadHeightMap(packetData);

            var res = ReadChunkResult(protocol, packetData);
            res.ChunkX = chunkX;
            res.ChunkZ = chunkZ;
            res.ChunksContinuous = chunksContinuous;
            res.ChunkMask = chunkMask;
            res.Dimension = protocol.Dimension();
            return res;
        }

        protected virtual ushort ReadChunkMask(List<byte> packetData)
        {
            return PacketUtils.readNextUShort(packetData);
        }

        protected virtual byte[] ReadHeightMap(List<byte> packetData)
        {
            return new byte[0];
        }

        protected virtual ChunkDataResult ReadChunkResult(IProtocol protocol, List<byte> packetData)
        {
            var res = new ChunkDataResult();

            var addBitmap = PacketUtils.readNextUShort(packetData);
            var compressedDataSize = PacketUtils.readNextInt(packetData);
            var compressed = PacketUtils.readData(compressedDataSize, packetData);
            var decompressed = ZlibUtils.Decompress(compressed);

            res.ChunkMask2 = addBitmap;
            res.HasSkyLights = 0 == protocol.Dimension();
            res.Cache = new List<byte>(decompressed);
            return res;
        }
    }
}