using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.WorldProcessors.BlockProcessors.Legacy
{
    internal class BlockProcessorLegacy : BlockProcessor
    {
        protected override int MinVersion => 0;

        public override IBlock CreateBlock(short blockId)
        {
            return new BlockLegacy(blockId);
        }

        public override IBlock CreateBlockFromMetadata(short type, byte metadata)
        {
            return new BlockLegacy(type, metadata);
        }

        public override IBlock CreateBlockFromIdMetadata(ushort typeAndMeta)
        {
            return new BlockLegacy(typeAndMeta);
        }

        public override IBlock CreateAirBlock()
        {
            return new BlockLegacy(Material.Air);
        }
    }
}