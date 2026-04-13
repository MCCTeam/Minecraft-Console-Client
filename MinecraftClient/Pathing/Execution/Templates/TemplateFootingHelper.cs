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

        public static bool IsCenterInsideTargetBlock(Location pos, Location target, double epsilon = 1.0E-4)
        {
            double blockMinX = Math.Floor(target.X);
            double blockMaxX = blockMinX + 1.0;
            double blockMinZ = Math.Floor(target.Z);
            double blockMaxZ = blockMinZ + 1.0;

            return pos.X >= blockMinX - epsilon
                && pos.X <= blockMaxX + epsilon
                && pos.Z >= blockMinZ - epsilon
                && pos.Z <= blockMaxZ + epsilon;
        }

        public static bool WillCenterLeaveTargetBlockNextTick(Location pos, PlayerPhysics physics, Location target, double epsilon = 1.0E-4)
        {
            Location nextPos = new(
                pos.X + physics.DeltaMovement.X,
                pos.Y,
                pos.Z + physics.DeltaMovement.Z);
            return !IsCenterInsideTargetBlock(nextPos, target, epsilon);
        }

        public static bool IsFootprintInsideSupportStrip(Location pos, Location first, Location second, double epsilon = 1.0E-4)
        {
            double minX = pos.X - HalfWidth;
            double maxX = pos.X + HalfWidth;
            double minZ = pos.Z - HalfWidth;
            double maxZ = pos.Z + HalfWidth;

            GetSupportStripBounds(first, second, out double stripMinX, out double stripMaxX, out double stripMinZ, out double stripMaxZ);

            return minX >= stripMinX - epsilon
                && maxX <= stripMaxX + epsilon
                && minZ >= stripMinZ - epsilon
                && maxZ <= stripMaxZ + epsilon;
        }

        public static bool WillLeaveSupportStripNextTick(Location pos, PlayerPhysics physics, Location first, Location second, double epsilon = 1.0E-4)
        {
            Location nextPos = new(
                pos.X + physics.DeltaMovement.X,
                pos.Y,
                pos.Z + physics.DeltaMovement.Z);
            return !IsFootprintInsideSupportStrip(nextPos, first, second, epsilon);
        }

        public static bool IsCenterInsideSupportStrip(Location pos, Location first, Location second, double epsilon = 1.0E-4)
        {
            GetSupportStripBounds(first, second, out double stripMinX, out double stripMaxX, out double stripMinZ, out double stripMaxZ);

            return pos.X >= stripMinX - epsilon
                && pos.X <= stripMaxX + epsilon
                && pos.Z >= stripMinZ - epsilon
                && pos.Z <= stripMaxZ + epsilon;
        }

        public static bool WillCenterLeaveSupportStripNextTick(Location pos, PlayerPhysics physics, Location first, Location second, double epsilon = 1.0E-4)
        {
            Location nextPos = new(
                pos.X + physics.DeltaMovement.X,
                pos.Y,
                pos.Z + physics.DeltaMovement.Z);
            return !IsCenterInsideSupportStrip(nextPos, first, second, epsilon);
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

        private static void GetSupportStripBounds(Location first, Location second,
            out double minX, out double maxX, out double minZ, out double maxZ)
        {
            double firstMinX = Math.Floor(first.X);
            double secondMinX = Math.Floor(second.X);
            double firstMinZ = Math.Floor(first.Z);
            double secondMinZ = Math.Floor(second.Z);

            minX = Math.Min(firstMinX, secondMinX);
            maxX = Math.Max(firstMinX, secondMinX) + 1.0;
            minZ = Math.Min(firstMinZ, secondMinZ);
            maxZ = Math.Max(firstMinZ, secondMinZ) + 1.0;
        }
    }
}
