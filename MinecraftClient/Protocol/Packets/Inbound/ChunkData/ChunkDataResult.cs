using System.Collections.Generic;

namespace MinecraftClient.Protocol.Packets.Inbound.ChunkData
{
    internal struct ChunkDataResult : IInboundData
    {
        public int ChunkX { get; internal set; }
        public int ChunkZ { get; internal set; }
        public ushort ChunkMask { get; internal set; }
        public ushort ChunkMask2 { get; internal set; }
        public bool HasSkyLights { get; internal set; }
        public bool ChunksContinuous { get; internal set; }
        public List<byte> Cache { get; internal set; }

        public int Dimension { get; internal set; }
    }
}