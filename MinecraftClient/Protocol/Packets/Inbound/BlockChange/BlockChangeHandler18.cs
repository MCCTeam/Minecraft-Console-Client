using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.WorldProcessors.BlockProcessors.Legacy;

namespace MinecraftClient.Protocol.Packets.Inbound.BlockChange
{
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

            handler.GetWorld().SetBlock(LocationFromLong(val), 
                handler.GetWorld().BlockProcessor.CreateBlockFromIdMetadata(blockIdMeta));
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
}