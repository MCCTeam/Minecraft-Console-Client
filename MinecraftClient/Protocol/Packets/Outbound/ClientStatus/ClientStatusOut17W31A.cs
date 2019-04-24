using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientStatus
{
    internal class ClientStatusOut17W31A : ClientStatusOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x03;
    }
}