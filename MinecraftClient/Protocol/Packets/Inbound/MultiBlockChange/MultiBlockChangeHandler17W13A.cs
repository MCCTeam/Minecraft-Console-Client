using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.MultiBlockChange
{
    internal class MultiBlockChangeHandler17W13A : MultiBlockChangeHandler19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x11;
    }
}