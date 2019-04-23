using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.BlockChange
{
    internal class BlockChangeHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x22;
        protected override InboundTypes PackageType => InboundTypes.BlockChange;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            if (!Settings.TerrainAndMovements)
            {
                return null;
            }

            var blockX = PacketUtils.readNextInt(packetData);
            var blockY = PacketUtils.readNextByte(packetData);
            var blockZ = PacketUtils.readNextInt(packetData);
            var blockId = (short) PacketUtils.readNextVarInt(packetData);
            var blockMeta = PacketUtils.readNextByte(packetData);
            handler.GetWorld().SetBlock(new Location(blockX, blockY, blockZ),
                handler.GetWorld().BlockProcessor.CreateBlockFromMetadata(blockId, blockMeta));
            return null;
        }
    }
}