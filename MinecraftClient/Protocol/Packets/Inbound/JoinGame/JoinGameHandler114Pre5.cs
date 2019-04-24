using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler114Pre5 : JoinGameHandler18W01A
    {
        protected override int MinVersion =>PacketUtils.MC114pre5Version;
        

        protected override byte ReadDifficulty(List<byte> packetData)
        {
            return 0;
        }

        protected override int ReadViewDistance(List<byte> packetData)
        {
            return PacketUtils.readNextVarInt(packetData);
        }
    }
}