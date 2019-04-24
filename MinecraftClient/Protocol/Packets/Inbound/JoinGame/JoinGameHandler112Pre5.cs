using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler112Pre5 : JoinGameHandler17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;

        protected override int PacketId => 0x23;
    }
}