using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.MultiBlockChange
{
    internal class MultiBlockChangeHandler17W31A : MultiBlockChangeHandler112Pre5
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x0F;
    }
}