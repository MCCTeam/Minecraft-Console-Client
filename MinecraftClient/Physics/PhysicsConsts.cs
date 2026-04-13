using System;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// All physics constants matching vanilla Minecraft 1.21.11.
    /// Values sourced from Entity.java, LivingEntity.java, Player.java, LocalPlayer.java.
    /// </summary>
    public static class PhysicsConsts
    {
        // --- Player dimensions per pose (vanilla Avatar.POSES, 26.1) ---
        public const double PlayerWidth = 0.6;
        public const double PlayerStandingHeight = 1.8;
        public const double PlayerStandingEyeHeight = 1.62;
        public const double PlayerCrouchingHeight = 1.5;
        public const double PlayerCrouchingEyeHeight = 1.27;
        public const double PlayerSwimmingHeight = 0.6;
        public const double PlayerSwimmingEyeHeight = 0.4;

        [Obsolete("Use PlayerStandingHeight instead")]
        public const double PlayerHeight = PlayerStandingHeight;
        [Obsolete("Use PlayerStandingEyeHeight instead")]
        public const double PlayerEyeHeight = PlayerStandingEyeHeight;

        // --- Gravity ---
        public const double DefaultGravity = 0.08;
        public const double SlowFallingCap = 0.01;

        // --- Step height ---
        public const float StepHeight = 0.6f;

        // --- Friction / drag ---
        public const float FrictionMultiplier = 0.91f;
        public const float DragY = 0.98f;
        public const float InputFriction = 0.98f;
        public const float GroundAccelerationFactor = 0.21600002f; // 0.216 / (f^3)
        public const float AirAcceleration = 0.02f;

        // --- Default block friction ---
        public const float DefaultBlockFriction = 0.6f;
        public const float IceFriction = 0.98f;
        public const float PackedIceFriction = 0.98f;
        public const float BlueIceFriction = 0.989f;
        public const float SlimeBlockFriction = 0.8f;

        // --- Speed factors ---
        public const float DefaultSpeedFactor = 1.0f;
        public const float SoulSandSpeedFactor = 0.4f;
        public const float HoneySpeedFactor = 0.4f;

        // --- Water ---
        public const float WaterSlowDown = 0.8f;
        public const float WaterSprintSlowDown = 0.9f;
        public const float DolphinsGraceSlowDown = 0.96f;
        public const float WaterBaseSpeed = 0.02f;
        public const float WaterYDamping = 0.8f;
        public const float WaterFloatImpulse = 0.04f;

        // --- Lava ---
        public const float LavaSpeed = 0.02f;
        public const double LavaHorizontalDamping = 0.5;
        public const double LavaVerticalDamping = 0.8;

        // --- Jump ---
        public const float BaseJumpPower = 0.42f;
        public const double SprintJumpHorizontalBoost = 0.2;

        // --- Climb ---
        public const float ClimbMaxSpeed = 0.15f;
        public const double ClimbWallBump = 0.2;

        // --- Velocity zeroing thresholds (from LivingEntity.aiStep) ---
        public const double PlayerHorizontalVelocityThresholdSqr = 9.0E-6; // < 0.003 length
        public const double NonPlayerVelocityThreshold = 0.003;
        public const double VerticalVelocityThreshold = 0.003;

        // --- Collision epsilon ---
        public const double CollisionEpsilon = 1.0E-7;

        // --- Position packet sending (from LocalPlayer.sendPosition) ---
        public const double PositionSendThresholdSqr = 4.0E-8; // (2e-4)^2
        public const int PositionReminderInterval = 20;

        // --- Flying detection ---
        public const double FloatingYThreshold = -0.03125;

        // --- Elytra ---
        public const double ElytraXZDrag = 0.99;
        public const double ElytraYDrag = 0.98;

        // --- Creative/spectator fly ---
        public const float DefaultFlySpeed = 0.05f;
        public const double FlyVerticalDamping = 0.6;
        public const double FlyVerticalBoostScale = 3.0;
    }
}
