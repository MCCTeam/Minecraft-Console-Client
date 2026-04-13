using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    public static class TemplateFootingHelper
    {
        private const double HalfWidth = PhysicsConsts.PlayerWidth / 2.0;

        public static bool IsFootprintInsideTargetBlock(Location pos, Location target, double epsilon = 1.0E-4)
        {
            double minX = pos.X - HalfWidth;
            double maxX = pos.X + HalfWidth;
            double minZ = pos.Z - HalfWidth;
            double maxZ = pos.Z + HalfWidth;

            double blockMinX = Math.Floor(target.X);
            double blockMaxX = blockMinX + 1.0;
            double blockMinZ = Math.Floor(target.Z);
            double blockMaxZ = blockMinZ + 1.0;

            return minX >= blockMinX - epsilon
                && maxX <= blockMaxX + epsilon
                && minZ >= blockMinZ - epsilon
                && maxZ <= blockMaxZ + epsilon;
        }

        public static bool WillLeaveTargetBlockNextTick(Location pos, PlayerPhysics physics, Location target, double epsilon = 1.0E-4)
        {
            Location nextPos = new(
                pos.X + physics.DeltaMovement.X,
                pos.Y,
                pos.Z + physics.DeltaMovement.Z);
            return !IsFootprintInsideTargetBlock(nextPos, target, epsilon);
        }

        public static bool WillCrossSupportExitNextTick(Location pos, PlayerPhysics physics, PathSegment segment, double epsilon = 1.0E-4)
        {
            double nextX = pos.X + physics.DeltaMovement.X;
            double nextZ = pos.Z + physics.DeltaMovement.Z;

            double blockMinX = Math.Floor(segment.End.X);
            double blockMaxX = blockMinX + 1.0;
            double blockMinZ = Math.Floor(segment.End.Z);
            double blockMaxZ = blockMinZ + 1.0;

            if (segment.HeadingX > 0 && nextX > blockMaxX - HalfWidth + epsilon)
                return true;
            if (segment.HeadingX < 0 && nextX < blockMinX + HalfWidth - epsilon)
                return true;
            if (segment.HeadingZ > 0 && nextZ > blockMaxZ - HalfWidth + epsilon)
                return true;
            if (segment.HeadingZ < 0 && nextZ < blockMinZ + HalfWidth - epsilon)
                return true;

            return false;
        }
    }
}
