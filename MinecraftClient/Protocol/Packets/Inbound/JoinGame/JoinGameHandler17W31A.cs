using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler17W31A : JoinGameHandler112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;


        protected override int PacketId => 0x24;
    }
}