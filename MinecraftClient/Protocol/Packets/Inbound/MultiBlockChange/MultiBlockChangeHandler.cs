using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.MultiBlockChange
{
    internal class MultiBlockChangeHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x22;
        protected override InboundTypes PackageType => InboundTypes.MultiBlockChange;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            var chunkX = PacketUtils.readNextInt(packetData);
            var chunkZ = PacketUtils.readNextInt(packetData);
            var recordCount = ReadRecordsCount(packetData);

            for (int ii = 0; ii < recordCount; ii++)
            {
                ReadAndUpdateBlock(handler, packetData, chunkX, chunkZ);
            }

            return null;
        }

        protected virtual int ReadRecordsCount(List<byte> packetData)
        {
            return PacketUtils.readNextShort(packetData);
        }

        protected virtual void ReadAndUpdateBlock(IMinecraftComHandler handler, List<byte> packetData, int chunkX,
            int chunkZ)
        {
            var blockIdMeta = PacketUtils.readNextUShort(packetData);
            var blockY = (ushort) PacketUtils.readNextByte(packetData);
            var locationXZ = PacketUtils.readNextByte(packetData);

            UpdateBlock(handler, chunkX, chunkZ, blockIdMeta, blockY, locationXZ);
        }

        protected virtual void UpdateBlock(IMinecraftComHandler handler, int chunkX, int chunkZ,
            ushort blockIdMeta, ushort blockY, byte locationXZ)
        {
            var blockX = locationXZ >> 4;
            var blockZ = locationXZ & 0x0F;
            var block = new Block(blockIdMeta);
            handler.GetWorld().SetBlock(new Location(chunkX, chunkZ, blockX, blockY, blockZ), block);
        }
    }
}