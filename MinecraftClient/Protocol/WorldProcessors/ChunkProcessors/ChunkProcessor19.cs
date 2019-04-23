using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Packets.Inbound.ChunkData;

namespace MinecraftClient.Protocol.WorldProcessors.ChunkProcessors
{
    internal class ChunkProcessor19 : ChunkProcessor
    {
        protected override int MinVersion => PacketUtils.MC19Version;

        public override void Process(IMinecraftComHandler handler, ChunkDataResult data)
        {
            // 1.9 and above chunk format
            // Unloading chunks is handled by a separate packet
            for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
            {
                if ((data.ChunkMask & (1 << chunkY)) != 0)
                {
                    byte bitsPerBlock = PacketUtils.readNextByte(data.Cache);
                    bool usePalette = (bitsPerBlock <= 8);

                    int paletteLength = PacketUtils.readNextVarInt(data.Cache);
                    int[] palette = new int[paletteLength];
                    for (int i = 0; i < paletteLength; i++)
                    {
                        palette[i] = PacketUtils.readNextVarInt(data.Cache);
                    }

                    // Bit mask covering bitsPerBlock bits
                    // EG, if bitsPerBlock = 5, valueMask = 00011111 in binary
                    uint valueMask = (uint) ((1 << bitsPerBlock) - 1);

                    ulong[] dataArray = PacketUtils.readNextULongArray(data.Cache);

                    Chunk chunk = new Chunk();

                    if (dataArray.Length > 0)
                    {
                        for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                        {
                            for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                            {
                                for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                {
                                    int blockNumber = (blockY * Chunk.SizeZ + blockZ) * Chunk.SizeX + blockX;

                                    int startLong = (blockNumber * bitsPerBlock) / 64;
                                    int startOffset = (blockNumber * bitsPerBlock) % 64;
                                    int endLong = ((blockNumber + 1) * bitsPerBlock - 1) / 64;

                                    // TODO: In the future a single ushort may not store the entire block id;
                                    // the Block code may need to change.
                                    ushort blockId;
                                    if (startLong == endLong)
                                    {
                                        blockId = (ushort) ((dataArray[startLong] >> startOffset) & valueMask);
                                    }
                                    else
                                    {
                                        int endOffset = 64 - startOffset;
                                        blockId = (ushort) ((dataArray[startLong] >> startOffset |
                                                             dataArray[endLong] << endOffset) & valueMask);
                                    }

                                    if (usePalette)
                                    {
                                        // Get the real block ID out of the palette
                                        blockId = (ushort) palette[blockId];
                                    }
                                    chunk[blockX, blockY, blockZ] = handler.GetWorld().BlockProcessor.CreateBlockFromIdMetadata(blockId);
                                }
                            }
                        }
                    }

                    //We have our chunk, save the chunk into the world
                    if (handler.GetWorld()[data.ChunkX, data.ChunkZ] == null)
                        handler.GetWorld()[data.ChunkX, data.ChunkZ] = new ChunkColumn();
                    handler.GetWorld()[data.ChunkX, data.ChunkZ][chunkY] = chunk;

                    //Skip block light
                    PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, data.Cache);

                    //Skip sky light
                    if (data.Dimension == 0)
                        // Sky light is not sent in the nether or the end
                        PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, data.Cache);
                }
            }

            // Don't worry about skipping remaining data since there is no useful data afterwards in 1.9
            // (plus, it would require parsing the tile entity lists' NBT)
        }
    }
}