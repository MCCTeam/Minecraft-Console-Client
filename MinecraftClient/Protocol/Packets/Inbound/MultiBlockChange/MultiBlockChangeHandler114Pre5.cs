using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.MultiBlockChange
{
    internal class MultiBlockChangeHandler114Pre5 : MultiBlockChangeHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;

        protected override void UpdateBlock(IMinecraftComHandler handler, int chunkX,
            int chunkZ, ushort blockIdMeta, ushort blockY,
            byte locationXz)
        {
            var worldX = (locationXz >> 4 & 15) + chunkX * 16;
            var worldZ = (locationXz & 15) + chunkZ * 16;

            //block ID now has global palette ID
            var block = handler.GetWorld().BlockProcessor.CreateBlock((short) blockIdMeta);
            handler.GetWorld().SetBlock(new Location(worldX, blockY, worldZ), block);
        }
    }
}