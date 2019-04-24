using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler19 : JoinGameHandler18
    {
        protected override int MinVersion => PacketUtils.MC19Version;


        protected override int PacketId => 0x23;
    }
}