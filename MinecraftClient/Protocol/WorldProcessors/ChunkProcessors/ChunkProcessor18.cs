using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Packets.Inbound.ChunkData;

namespace MinecraftClient.Protocol.WorldProcessors.ChunkProcessors
{
    internal class ChunkProcessor18 : ChunkProcessor
    {
        protected override int MinVersion => PacketUtils.MC18Version;

        public override void Process(IMinecraftComHandler handler, ChunkDataResult data)
        {
            if (data.ChunksContinuous && data.ChunkMask == 0)
            {
                //Unload the entire chunk column
                handler.GetWorld()[data.ChunkX, data.ChunkZ] = null;
            }
            else
            {
                //Load chunk data from the server
                for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                {
                    if ((data.ChunkMask & (1 << chunkY)) != 0)
                    {
                        Chunk chunk = new Chunk();

                        //Read chunk data, all at once for performance reasons, and build the chunk object
                        Queue<ushort> queue = new Queue<ushort>(
                            PacketUtils.readNextUShortsLittleEndian(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ,
                                data.Cache));
                        for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                        for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                        for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                            chunk[blockX, blockY, blockZ] = handler.GetWorld().BlockProcessor.CreateBlockFromIdMetadata(queue.Dequeue());

                        //We have our chunk, save the chunk into the world
                        if (handler.GetWorld()[data.ChunkX, data.ChunkZ] == null)
                            handler.GetWorld()[data.ChunkX, data.ChunkZ] = new ChunkColumn();
                        handler.GetWorld()[data.ChunkX, data.ChunkZ][chunkY] = chunk;
                    }
                }

                //Skip light information
                for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                {
                    if ((data.ChunkMask & (1 << chunkY)) != 0)
                    {
                        //Skip block light
                        PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, data.Cache);

                        //Skip sky light
                        if (data.HasSkyLights)
                            PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, data.Cache);
                    }
                }

                //Skip biome metadata
                if (data.ChunksContinuous)
                    PacketUtils.readData(Chunk.SizeX * Chunk.SizeZ, data.Cache);
            }
        }
    }
}