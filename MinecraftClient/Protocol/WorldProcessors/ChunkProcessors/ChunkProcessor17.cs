using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Packets.Inbound.ChunkData;
using MinecraftClient.Protocol.WorldProcessors.BlockProcessors;
using MinecraftClient.Protocol.WorldProcessors.BlockProcessors.Legacy;

namespace MinecraftClient.Protocol.WorldProcessors.ChunkProcessors
{
    /// <summary>
    /// 1.7 chunk format
    /// </summary>
    internal class ChunkProcessor17 : ChunkProcessor
    {
        protected override int MinVersion => 0;

        public override void Process(IMinecraftComHandler handler, ChunkDataResult data)
        {
            if (data.ChunksContinuous && data.ChunkMask == 0)
            {
                //Unload the entire chunk column
                handler.GetWorld()[data.ChunkX, data.ChunkZ] = null;
            }
            else
            {
                //Count chunk sections
                int sectionCount = 0;
                int addDataSectionCount = 0;
                for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                {
                    if ((data.ChunkMask & (1 << chunkY)) != 0)
                        sectionCount++;
                    if ((data.ChunkMask2 & (1 << chunkY)) != 0)
                        addDataSectionCount++;
                }

                //Read chunk data, unpacking 4-bit values into 8-bit values for block metadata
                Queue<byte> blockTypes =
                    new Queue<byte>(PacketUtils.readData(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount,
                        data.Cache));
                Queue<byte> blockMeta = new Queue<byte>();
                foreach (byte packed in PacketUtils.readData(
                    (Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, data.Cache))
                {
                    byte hig = (byte) (packed >> 4);
                    byte low = (byte) (packed & (byte) 0x0F);
                    blockMeta.Enqueue(hig);
                    blockMeta.Enqueue(low);
                }

                //Skip data we don't need
                PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2,
                    data.Cache); //Block light
                if (data.HasSkyLights)
                    PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2,
                        data.Cache); //Sky light
                PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * addDataSectionCount) / 2,
                    data.Cache); //BlockAdd
                if (data.ChunksContinuous)
                    PacketUtils.readData(Chunk.SizeX * Chunk.SizeZ, data.Cache); //Biomes

                //Load chunk data
                for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                {
                    if ((data.ChunkMask & (1 << chunkY)) != 0)
                    {
                        Chunk chunk = new Chunk();

                        for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                        for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                        for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                            chunk[blockX, blockY, blockZ] = handler.GetWorld().BlockProcessor
                                .CreateBlockFromMetadata(blockTypes.Dequeue(), blockMeta.Dequeue());

                        if (handler.GetWorld()[data.ChunkX, data.ChunkZ] == null)
                            handler.GetWorld()[data.ChunkX, data.ChunkZ] = new ChunkColumn();
                        handler.GetWorld()[data.ChunkX, data.ChunkZ][chunkY] = chunk;
                    }
                }
            }
        }
    }
}