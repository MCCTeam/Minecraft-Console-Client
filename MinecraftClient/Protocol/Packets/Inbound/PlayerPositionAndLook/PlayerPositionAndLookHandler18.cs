using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookHandler18 : PlayerPositionAndLookHandler
    {
        protected override int MinVersion => PacketUtils.MC18Version;
        protected override void UpdateLocation(IMinecraftComHandler handler, double x, double y, double z, float yaw, float pitch, byte locMask)
        {
            var location = handler.GetCurrentLocation();
            location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
            location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
            location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
            handler.UpdateLocation(location, yaw, pitch);
        }
    }
}