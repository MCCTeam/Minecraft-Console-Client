using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.BlockChange
{
    internal class BlockChangeHandler19 : BlockChangeHandler18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x10;
    }
}