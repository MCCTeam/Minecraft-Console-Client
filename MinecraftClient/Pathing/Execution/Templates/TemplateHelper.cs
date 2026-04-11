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

        /// <summary>
        /// Calculate the pitch angle (in degrees) to look toward a 3D offset.
        /// Negative = look up, positive = look down. Clamped to [-90, 90].
        /// The dy is relative to eye height (~1.62 blocks above feet).
        /// </summary>
        internal static float CalculatePitch(double dx, double dy, double dz)
        {
            double horizDist = Math.Sqrt(dx * dx + dz * dz);
            float pitch = (float)(-Math.Atan2(dy, horizDist) / Math.PI * 180.0);
            return Math.Clamp(pitch, -90f, 90f);
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
