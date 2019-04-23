using MinecraftClient.Protocol.Packets.Inbound.ChunkData;

namespace MinecraftClient.Protocol.WorldProcessors.ChunkProcessors
{
    internal abstract class ChunkProcessor: IChunkProcessor
    {   
        protected abstract int MinVersion { get; } 
        
        int IWorldProcessor.MinVersion()
        {
            return MinVersion;
        }

        public abstract void Process(IMinecraftComHandler handler, ChunkDataResult data);
    }
}