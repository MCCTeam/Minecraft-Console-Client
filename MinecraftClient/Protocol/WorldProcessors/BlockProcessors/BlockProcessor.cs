namespace MinecraftClient.Protocol.WorldProcessors.BlockProcessors
{
    internal abstract class BlockProcessor : IBlockProcessor
    {
        protected abstract int MinVersion { get; }

        int IWorldProcessor.MinVersion()
        {
            return MinVersion;
        }

        public abstract IBlock CreateBlock(short blockId);

        public abstract IBlock CreateBlockFromMetadata(short type, byte metadata);
        public abstract IBlock CreateBlockFromIdMetadata(ushort typeAndMeta);
        public abstract IBlock CreateAirBlock();
    }
}