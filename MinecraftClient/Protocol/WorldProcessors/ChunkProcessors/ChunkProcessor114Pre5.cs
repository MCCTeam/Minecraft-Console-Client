using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Packets.Inbound.ChunkData;

namespace MinecraftClient.Protocol.WorldProcessors.ChunkProcessors
{
    internal class ChunkProcessor114Pre5 : ChunkProcessor
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;

        public override void Process(IMinecraftComHandler handler, ChunkDataResult data)
        {
            for (var chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
            {
                if (0 == (data.ChunkMask & (1 << chunkY)))
                {
                    continue;
                }

                var blockCount = PacketUtils.readNextShort(data.Cache);
                var bitsPerBlock = PacketUtils.readNextByte(data.Cache);
                var palette = PacketUtils.readNextVarIntArray(data.Cache);
                
                var blockData = PacketUtils.readNextULongArray(data.Cache);

                var isGlobalPalette = false;
                if (bitsPerBlock < 4)
                {
                    bitsPerBlock = 4;
                }

                if (bitsPerBlock > 8)
                {
                    isGlobalPalette = true;
                    bitsPerBlock = 14;
                }

                var valueMask = (uint) ((1 << bitsPerBlock) - 1);

                var chunk = new Chunk();

                if (blockData.Length <= 0)
                {
                    continue;
                }

                for (var blockY = 0; blockY < Chunk.SizeY; blockY++)
                {
                    for (var blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                    {
                        for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                        {
                            int blockNumber = (blockY * Chunk.SizeZ + blockZ) * Chunk.SizeX + blockX;

                            int startLong = blockNumber * bitsPerBlock / 64;
                            int startOffset = blockNumber * bitsPerBlock % 64;
                            int endLong = ((blockNumber + 1) * bitsPerBlock - 1) / 64;

                            short blockId;
                            if (startLong == endLong)
                            {
                                blockId = (short) ((blockData[startLong] >> startOffset) & valueMask);
                            }
                            else
                            {
                                int endOffset = 64 - startOffset;
                                blockId = (short) ((blockData[startLong] >> startOffset |
                                                     blockData[endLong] << endOffset) & valueMask);
                            }

                            if (!isGlobalPalette)
                            {
                                blockId = (short) palette[blockId];
                            }

                            chunk[blockX, blockY, blockZ] = handler.GetWorld().BlockProcessor.CreateBlock(blockId);
                        }
                    }
                }

                //We have our chunk, save the chunk into the world
                if (handler.GetWorld()[data.ChunkX, data.ChunkZ] == null)
                    handler.GetWorld()[data.ChunkX, data.ChunkZ] = new ChunkColumn();
                handler.GetWorld()[data.ChunkX, data.ChunkZ][chunkY] = chunk;
            }
        }
    }
}