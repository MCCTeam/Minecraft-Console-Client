using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.JoinGame
{
    internal class JoinGameHandler18W01A : JoinGameHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC18w01aVersion;


        protected override int PacketId => 0x25;
    }
}