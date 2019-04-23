using MinecraftClient.Protocol.Packets.Inbound.ChunkData;

namespace MinecraftClient.Protocol.WorldProcessors.ChunkProcessors
{
    internal interface IChunkProcessor: IWorldProcessor
    {
        void Process(IMinecraftComHandler handler, ChunkDataResult data);
    }
}