using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Kick
{
    internal class KickHandler19 : KickHandler
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x1A;
    }
}