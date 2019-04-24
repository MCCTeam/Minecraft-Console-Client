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
            handler.GetWorld().SetBlock(new Location(blockX, blockY, blockZ), new Block(blockId, blockMeta));
            return null;
        }
    }

    internal class BlockChangeHandler18 : BlockChangeHandler
    {
        protected override int MinVersion => PacketUtils.MC18Version;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            if (!Settings.TerrainAndMovements)
            {
                return null;
            }

            var val = (long) PacketUtils.readNextULong(packetData);
            var blockIdMeta = (ushort) PacketUtils.readNextVarInt(packetData);

            handler.GetWorld().SetBlock(LocationFromLong(val), new Block(blockIdMeta));
            return null;
        }

        protected virtual Location LocationFromLong(long val)
        {
            var x = (int) (val >> 38);
            var y = (int) ((val >> 26) & 0xFFF);
            var z = (int) (val << 38 >> 38);
            if (x >= 33554432)
                x -= 67108864;
            if (y >= 2048)
                y -= 4096;
            if (z >= 33554432)
                z -= 67108864;
            return new Location(x, y, z);
        }
    }

    internal class BlockChangeHandler19 : BlockChangeHandler18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x10;
    }

    internal class BlockChangeHandler17W13A : BlockChangeHandler19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x11;
    }

    internal class BlockChangeHandler112Pre5 : BlockChangeHandler17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x10;
    }

    internal class BlockChangeHandler17W31A : BlockChangeHandler112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x0F;
    }

    internal class BlockChangeHandler114Pre5 : BlockChangeHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x0B;

        protected override Location LocationFromLong(long val)
        {
            var x = (int) (val >> 38);
            var y = (int) (val >> 26 & 0xFFF);
            var z = (int) ((val << 38 >> 38) >> 12);
            if (x >= 33554432)
                x -= 67108864;
            if (y >= 2048)
                y -= 4096;
            if (z >= 33554432)
                z -= 67108864;
            return new Location(x, y, z);
        }
    }
}