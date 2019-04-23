using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.WorldProcessors.BlockProcessors._114Pre5
{
    internal class BlockProcessor114Pre5 : BlockProcessor
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;

        public override IBlock CreateBlock(short blockId)
        {
            return new Block114Pre5(blockId);
        }

        public override IBlock CreateBlockFromMetadata(short type, byte metadata)
        {
            throw new System.NotImplementedException();
        }

        public override IBlock CreateBlockFromIdMetadata(ushort typeAndMeta)
        {
            throw new System.NotImplementedException();
        }

        public override IBlock CreateAirBlock()
        {
            return new Block114Pre5(Material.Air);
        }
    }
}