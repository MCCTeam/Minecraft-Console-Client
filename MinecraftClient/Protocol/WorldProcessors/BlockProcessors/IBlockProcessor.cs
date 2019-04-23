namespace MinecraftClient.Protocol.WorldProcessors.BlockProcessors
{
    internal interface IBlockProcessor: IWorldProcessor
    {
        IBlock CreateBlock(short blockId);
        IBlock CreateBlockFromMetadata(short type, byte metadata);
        
        IBlock CreateBlockFromIdMetadata(ushort typeAndMeta);

        IBlock CreateAirBlock();
    }
}