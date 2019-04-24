using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Kick
{
    internal class KickHandler114Pre5 : KickHandler17W31A
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x1A;
    }
}