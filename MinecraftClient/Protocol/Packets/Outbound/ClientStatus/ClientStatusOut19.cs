using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientStatus
{
    internal class ClientStatusOut19 : ClientStatusOut
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x03;
    }
}