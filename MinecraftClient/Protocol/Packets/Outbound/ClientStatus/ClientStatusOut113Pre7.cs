using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientStatus
{
    internal class ClientStatusOut113Pre7 : ClientStatusOut17W45A
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x03;
    }
}