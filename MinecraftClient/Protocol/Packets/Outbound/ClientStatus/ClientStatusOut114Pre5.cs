using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientStatus
{
    internal class ClientStatusOut114Pre5 : ClientStatusOut113Pre7
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x04;
    }
}