using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler17W13A : JoinGameHandler191
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;

        protected override int PacketId => 0x24;
    }
}