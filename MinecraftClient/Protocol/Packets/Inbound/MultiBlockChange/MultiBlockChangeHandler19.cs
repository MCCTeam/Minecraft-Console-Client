using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.MultiBlockChange
{
    internal class MultiBlockChangeHandler19 : MultiBlockChangeHandler18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x10;
    }
}