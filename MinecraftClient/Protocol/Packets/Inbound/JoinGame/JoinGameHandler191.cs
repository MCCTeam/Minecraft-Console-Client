using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler191 : JoinGameHandler19
    {
        protected override int MinVersion => PacketUtils.MC191Version;


        protected override int ReadDimension(List<byte> packetData)
        {
            return PacketUtils.readNextInt(packetData);
        }
    }
}