using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal struct PlayerPositionRequest : IOutboundRequest
    {
        public Location Location { get; set; }
        public bool IsOnGround { get; set; }
    }
}