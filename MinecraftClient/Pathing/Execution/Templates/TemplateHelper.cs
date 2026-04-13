using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    internal static class TemplateHelper
    {
        private const double EyeHeight = 1.62;
        private const float MaxYawStepPerTick = 35f;
        private const float MaxPitchStepPerTick = 25f;

        internal static float CalculateYaw(double dx, double dz)
        {
            float yaw = (float)(-Math.Atan2(dx, dz) / Math.PI * 180.0);
            if (yaw < 0) yaw += 360;
            return yaw;
        }

        /// <summary>
        /// Calculate the pitch angle to look from current eye position toward
        /// the target's feet-level Y. dy = targetFeetY - playerFeetY.
        /// </summary>
        internal static float CalculatePitch(double dx, double dy, double dz)
        {
            double horizDist = Math.Sqrt(dx * dx + dz * dz);
            // Look toward the target's eye level, not feet.
            // Both player and target are at feet+EyeHeight, so the vertical
            // difference is just dy (target feet Y - player feet Y).
            float pitch = (float)(-Math.Atan2(dy, horizDist) / Math.PI * 180.0);
            return Math.Clamp(pitch, -90f, 90f);
        }

        /// <summary>
        /// Smoothly interpolate yaw toward a target, respecting wrap-around at 0/360.
        /// </summary>
        internal static float SmoothYaw(float current, float target, float maxStep = MaxYawStepPerTick)
        {
            float delta = target - current;
            // Normalize to [-180, 180]
            while (delta > 180f) delta -= 360f;
            while (delta < -180f) delta += 360f;

            if (Math.Abs(delta) <= maxStep)
                return target;

            float result = current + Math.Sign(delta) * maxStep;
            if (result < 0) result += 360f;
            if (result >= 360f) result -= 360f;
            return result;
        }

        /// <summary>
        /// Smoothly interpolate pitch toward a target.
        /// </summary>
        internal static float SmoothPitch(float current, float target, float maxStep = MaxPitchStepPerTick)
        {
            float delta = target - current;
            if (Math.Abs(delta) <= maxStep)
                return target;
            return current + Math.Sign(delta) * maxStep;
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

        internal static void FaceSegmentHeading(PlayerPhysics physics, PathSegment segment)
        {
            float headingYaw = CalculateYaw(segment.HeadingX, segment.HeadingZ);
            physics.Yaw = SmoothYaw(physics.Yaw, headingYaw);
        }

        internal static void FaceExitHeading(PlayerPhysics physics, PathSegment segment)
        {
            float headingYaw = GetExitHeadingYaw(segment);
            physics.Yaw = SmoothYaw(physics.Yaw, headingYaw);
        }

        internal static void ApplyDecision(MovementInput input, TransitionBrakingDecision decision)
        {
            input.Forward = decision.HoldForward;
            input.Sprint = decision.HoldSprint;
            input.Back = decision.HoldBack;
        }

        internal static bool HasReachedSegmentEndPlane(Location pos, PathSegment segment, double tolerance = 0.05)
        {
            GetNormalizedSegmentDirection(segment, out double dirX, out double dirZ);
            double relX = pos.X - segment.End.X;
            double relZ = pos.Z - segment.End.Z;
            return relX * dirX + relZ * dirZ >= -tolerance;
        }

        internal static double ProjectHorizontalSpeedAlongSegment(PlayerPhysics physics, PathSegment segment)
        {
            GetNormalizedSegmentDirection(segment, out double dirX, out double dirZ);
            return physics.DeltaMovement.X * dirX + physics.DeltaMovement.Z * dirZ;
        }

        internal static double ProjectHorizontalSpeedAlongHint(PlayerPhysics physics, PathSegment segment)
        {
            GetExitHeading(segment, out int headingX, out int headingZ);
            return ProjectHorizontalSpeedAlongHeading(physics, headingX, headingZ);
        }

        internal static double ProjectHorizontalSpeedAlongHeading(PlayerPhysics physics, int headingX, int headingZ)
        {
            if (headingX == 0 && headingZ == 0)
                return GetHorizontalSpeed(physics);

            return physics.DeltaMovement.X * headingX + physics.DeltaMovement.Z * headingZ;
        }

        internal static double GetHorizontalSpeed(PlayerPhysics physics)
        {
            return Math.Sqrt(physics.DeltaMovement.X * physics.DeltaMovement.X
                + physics.DeltaMovement.Z * physics.DeltaMovement.Z);
        }

        internal static double RemainingDistanceAlongSegment(Location pos, PathSegment segment)
        {
            double dx = segment.End.X - pos.X;
            double dz = segment.End.Z - pos.Z;
            return dx * segment.HeadingX + dz * segment.HeadingZ;
        }

        internal static bool ShouldBiasTowardExitHeading(Location pos, PathSegment segment, double distanceThreshold = 0.35)
        {
            GetExitHeading(segment, out int headingX, out int headingZ);
            if ((headingX == 0 && headingZ == 0)
                || (headingX == segment.HeadingX && headingZ == segment.HeadingZ))
            {
                return false;
            }

            return RemainingDistanceAlongSegment(pos, segment) <= distanceThreshold;
        }

        internal static bool IsSettledOnTargetBlock(Location pos, Location target, PlayerPhysics physics,
            double speedThresholdSq = 0.0016)
        {
            double horizontalSpeedSq = physics.DeltaMovement.X * physics.DeltaMovement.X
                + physics.DeltaMovement.Z * physics.DeltaMovement.Z;
            return TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, target)
                && !TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, target)
                && horizontalSpeedSq <= speedThresholdSq;
        }

        internal static bool IsSettledAtEnd(Location pos, Location target, PlayerPhysics physics,
            double horizThresholdSq = 0.0025, double speedThresholdSq = 0.0016)
        {
            if (IsSettledOnTargetBlock(pos, target, physics, speedThresholdSq))
                return true;

            double horizontalSpeedSq = physics.DeltaMovement.X * physics.DeltaMovement.X
                + physics.DeltaMovement.Z * physics.DeltaMovement.Z;
            if (horizontalSpeedSq > speedThresholdSq)
                return false;

            if (TemplateFootingHelper.IsCenterInsideTargetBlock(pos, target)
                && !TemplateFootingHelper.WillCenterLeaveTargetBlockNextTick(pos, physics, target))
            {
                return true;
            }

            double dx = target.X - pos.X;
            double dz = target.Z - pos.Z;
            return dx * dx + dz * dz <= horizThresholdSq;
        }

        internal static double HeadingPenaltyDegrees(float yaw, PathSegment segment)
        {
            GetExitHeading(segment, out int headingX, out int headingZ);
            return HeadingPenaltyDegrees(yaw, headingX, headingZ);
        }

        internal static double HeadingPenaltyDegrees(float yaw, int headingX, int headingZ)
        {
            if (headingX == 0 && headingZ == 0)
                return 0.0;

            float targetYaw = CalculateYaw(headingX, headingZ);
            float delta = targetYaw - yaw;
            while (delta > 180f) delta -= 360f;
            while (delta < -180f) delta += 360f;
            return Math.Abs(delta);
        }

        internal static float GetExitHeadingYaw(PathSegment segment)
        {
            GetExitHeading(segment, out int headingX, out int headingZ);
            return CalculateYaw(headingX, headingZ);
        }

        internal static void GetExitHeading(PathSegment segment, out int headingX, out int headingZ)
        {
            headingX = segment.ExitHints.DesiredHeadingX;
            headingZ = segment.ExitHints.DesiredHeadingZ;

            if (headingX == 0 && headingZ == 0)
            {
                headingX = segment.HeadingX;
                headingZ = segment.HeadingZ;
            }
        }

        internal static PlayerPhysics ClonePhysicsForPlanning(PlayerPhysics physics)
        {
            return new PlayerPhysics
            {
                Position = physics.Position,
                DeltaMovement = physics.DeltaMovement,
                Yaw = physics.Yaw,
                Pitch = physics.Pitch,
                OnGround = physics.OnGround,
                HorizontalCollision = physics.HorizontalCollision,
                VerticalCollision = physics.VerticalCollision,
                VerticalCollisionBelow = physics.VerticalCollisionBelow,
                FallDistance = physics.FallDistance,
                StuckSpeedMultiplier = physics.StuckSpeedMultiplier,
                Xxa = physics.Xxa,
                Zza = physics.Zza,
                Yya = physics.Yya,
                Jumping = physics.Jumping,
                Sprinting = physics.Sprinting,
                Sneaking = physics.Sneaking,
                CreativeFlying = physics.CreativeFlying,
                InWater = physics.InWater,
                IsUnderWater = physics.IsUnderWater,
                InLava = physics.InLava,
                OnClimbable = physics.OnClimbable,
                HasSlowFalling = physics.HasSlowFalling,
                HasLevitation = physics.HasLevitation,
                LevitationAmplifier = physics.LevitationAmplifier,
                MovementSpeed = physics.MovementSpeed
            };
        }

        private static void GetNormalizedSegmentDirection(PathSegment segment, out double dirX, out double dirZ)
        {
            dirX = segment.End.X - segment.Start.X;
            dirZ = segment.End.Z - segment.Start.Z;
            double len = Math.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len < 1.0E-6)
            {
                dirX = 0.0;
                dirZ = 0.0;
                return;
            }

            dirX /= len;
            dirZ /= len;
        }
    }
}
