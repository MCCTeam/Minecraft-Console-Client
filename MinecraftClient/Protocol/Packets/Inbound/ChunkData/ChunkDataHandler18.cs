using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChunkData
{
    internal class ChunkDataHandler18 : ChunkDataHandler
    {
        protected override int MinVersion => PacketUtils.MC18Version;

        protected override ChunkDataResult ReadChunkResult(IProtocol protocol, List<byte> packetData)
        {
            var res = new ChunkDataResult();
            PacketUtils.readNextVarInt(packetData); // data size
            res.ChunkMask2 = 0;
            res.HasSkyLights = false;
            res.Cache = packetData;

            return res;
        }
    }
}