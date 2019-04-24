using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    /// <summary>
    /// Implementation of the Clientbound Join Game Packet
    /// https://wiki.vg/Protocol#Join_Game
    /// </summary>
    internal class JoinGameHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x01;
        protected override InboundTypes PackageType => InboundTypes.JoinGame;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            var res = new JoinGameResult();
            handler.OnGameJoined();
            ReadEntityID(packetData);
            ReadGameMode(packetData);
            res.Dimension = ReadDimension(packetData);
            ReadDifficulty(packetData);
            ReadMaxPlayers(packetData);
            ReadLevelType(packetData);
            ReadViewDistance(packetData);
            ReadReducedDebugInfo(packetData);
            return res;
        }

        protected virtual int ReadEntityID(List<byte> packetData)
        {
            return PacketUtils.readNextInt(packetData);
        }

        protected virtual byte ReadGameMode(List<byte> packetData)
        {
            return PacketUtils.readNextByte(packetData);
        }

        protected virtual byte ReadDifficulty(List<byte> packetData)
        {
            return PacketUtils.readNextByte(packetData);
        }

        protected virtual int ReadDimension(List<byte> packetData)
        {
            return (sbyte) PacketUtils.readNextByte(packetData);
        }

        protected virtual byte ReadMaxPlayers(List<byte> packetData)
        {
            return PacketUtils.readNextByte(packetData);
        }

        protected virtual string ReadLevelType(List<byte> packetData)
        {
            return PacketUtils.readNextString(packetData);
        }

        protected virtual bool ReadReducedDebugInfo(List<byte> packetData)
        {
            return false;
        }

        protected virtual int ReadViewDistance(List<byte> packetData)
        {
            return 0;
        }
    }
}