using System;
using MinecraftClient.Mapping;

namespace MinecraftClient.Pathing.Execution.Templates
{
    internal static class TemplateHelper
    {
        internal static float CalculateYaw(double dx, double dz)
        {
            float yaw = (float)(-Math.Atan2(dx, dz) / Math.PI * 180.0);
            if (yaw < 0) yaw += 360;
            return yaw;
        }

        internal static double HorizontalDistanceSq(Location a, Location b)
        {
            double dx = a.X - b.X;
            double dz = a.Z - b.Z;
            return dx * dx + dz * dz;
        }

        internal static bool IsNear(Location pos, Location target,
            double horizThresholdSq = 0.25, double vertThreshold = 0.8)
        {
            double dx = target.X - pos.X;
            double dz = target.Z - pos.Z;
            double dy = target.Y - pos.Y;
            return dx * dx + dz * dz < horizThresholdSq && Math.Abs(dy) < vertThreshold;
        }
    }
}
