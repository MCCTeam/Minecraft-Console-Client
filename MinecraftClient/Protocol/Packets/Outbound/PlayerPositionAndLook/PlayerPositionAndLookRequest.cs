using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal struct PlayerPositionAndLookRequest : IOutboundRequest
    {
        public Location Location { get; set; }
        public bool IsOnGround { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
    }
}