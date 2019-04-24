using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    /// <summary>
    /// Implementation of the Clientbound Respawn Packet
    /// https://wiki.vg/Protocol#Respawn
    /// </summary>
    internal class RespawnHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x07;
        protected override InboundTypes PackageType => InboundTypes.Respawn;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            var res = new RespawnResult {Dimension = ReadDimension(packetData)};
            ReadDifficulty(packetData);
            res.GameMode = ReadGameMode(packetData);
            ReadLevelType(packetData);
            return res;
        }

        protected virtual int ReadDimension(List<byte> packetData)
        {
            return PacketUtils.readNextInt(packetData);
        }

        protected virtual byte ReadDifficulty(List<byte> packetData)
        {
            return PacketUtils.readNextByte(packetData);
        }

        protected virtual byte ReadGameMode(List<byte> packetData)
        {
            return PacketUtils.readNextByte(packetData);
        }

        protected virtual string ReadLevelType(List<byte> packetData)
        {
            return PacketUtils.readNextString(packetData);
        }
    }
}