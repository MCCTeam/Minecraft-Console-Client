using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Kick
{
    internal class KickHandler112Pre5 : KickHandler17W13A
    {
        protected override int MinVersion => PacketUtils.MC112pre5Version;
        protected override int PacketId => 0x1A;
    }
}