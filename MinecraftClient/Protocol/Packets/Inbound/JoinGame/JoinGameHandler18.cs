using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler18 : JoinGameHandler
    {
        protected override int MinVersion => PacketUtils.MC18Version;


        protected override bool ReadReducedDebugInfo(List<byte> packetData)
        {
            return PacketUtils.readNextBool(packetData);
        }
    }
}