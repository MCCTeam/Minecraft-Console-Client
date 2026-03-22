using System;
using MinecraftClient.Mapping;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Core physics tick engine for the player, faithfully replicating vanilla 1.21.11 physics.
    /// Mirrors the combined logic of Entity.move(), LivingEntity.aiStep()/travel()/travelInAir(),
    /// Player.travel(), and LocalPlayer.aiStep().
    /// </summary>
    public class PlayerPhysics
    {
        // --- State ---
        public Vec3d Position;
        public Vec3d DeltaMovement;
        public float Yaw;
        public float Pitch;
        public bool OnGround;
        public bool HorizontalCollision;
        public bool VerticalCollision;
        public bool VerticalCollisionBelow;
        public double FallDistance;
        public Vec3d StuckSpeedMultiplier = Vec3d.Zero;

        // Movement input
        public float Xxa; // strafe
        public float Zza; // forward
        public float Yya; // vertical (creative fly)
        public bool Jumping;

        // Movement mode flags
        public bool Sprinting;
        public bool Sneaking;
        public bool CreativeFlying;
        public bool InWater;
        public bool InLava;
        public bool OnClimbable;
        public bool HasSlowFalling;
        public bool HasLevitation;
        public int LevitationAmplifier;

        // Player dimensions
        public double PlayerWidth = PhysicsConsts.PlayerWidth;
        public double PlayerHeight = PhysicsConsts.PlayerHeight;

        // Anti-jump-spam
        private int noJumpDelay;

        // Tick counter for position packet timing
        public int TickCount;

        // Movement speed attribute (base = 0.1 for players)
        public float MovementSpeed = 0.1f;

        /// <summary>
        /// Get the player's bounding box at current position
        /// </summary>
        public Aabb GetBoundingBox()
        {
            return Aabb.OfSize(Position.X, Position.Y, Position.Z, PlayerWidth, PlayerHeight);
        }

        /// <summary>
        /// Run one physics tick. Call at 20 TPS.
        /// </summary>
        public void Tick(World world)
        {
            TickCount++;

            // Velocity threshold zeroing (LivingEntity.aiStep)
            ZeroTinyVelocity();

            // Jump handling
            HandleJumping(world);

            // Build travel input
            Vec3d travelInput = new(Xxa, Yya, Zza);

            // Travel (dispatches to air/water/lava/fly)
            Travel(world, travelInput);

            if (noJumpDelay > 0)
                noJumpDelay--;
        }

        /// <summary>
        /// Apply the movement input from MovementInput to xxa/zza.
        /// Call before Tick() each frame.
        /// </summary>
        public void ApplyInput(MovementInput input)
        {
            var (rawXxa, rawZza) = input.GetMoveVector();

            // Scale by INPUT_FRICTION (0.98) — this matches LocalPlayer.modifyInput
            rawXxa *= PhysicsConsts.InputFriction;
            rawZza *= PhysicsConsts.InputFriction;

            // Sneak slowdown
            if (input.Sneak)
            {
                rawXxa *= 0.3f;
                rawZza *= 0.3f;
            }

            Xxa = rawXxa;
            Zza = rawZza;
            Yya = 0;
            Jumping = input.Jump;
            Sneaking = input.Sneak;
            Sprinting = input.Sprint;

            // Creative/spectator fly vertical
            if (CreativeFlying)
            {
                if (input.Jump)
                    Yya += (float)(PhysicsConsts.DefaultFlySpeed * PhysicsConsts.FlyVerticalBoostScale);
                if (input.Sneak)
                    Yya -= (float)(PhysicsConsts.DefaultFlySpeed * PhysicsConsts.FlyVerticalBoostScale);
            }
        }

        private void ZeroTinyVelocity()
        {
            double dx = DeltaMovement.X;
            double dy = DeltaMovement.Y;
            double dz = DeltaMovement.Z;

            // Player-specific: zero horizontal if combined length < 0.003
            if (dx * dx + dz * dz < PhysicsConsts.PlayerHorizontalVelocityThresholdSqr)
            {
                dx = 0;
                dz = 0;
            }
            if (Math.Abs(dy) < PhysicsConsts.VerticalVelocityThreshold)
                dy = 0;

            DeltaMovement = new Vec3d(dx, dy, dz);
        }

        private void HandleJumping(World world)
        {
            if (!Jumping) { noJumpDelay = 0; return; }

            if (InWater || InLava)
            {
                // Jump in fluid: add upward impulse
                DeltaMovement = DeltaMovement.Add(0, PhysicsConsts.WaterFloatImpulse, 0);
            }
            else if (OnGround && noJumpDelay == 0)
            {
                JumpFromGround();
                noJumpDelay = 10;
            }
        }

        private void JumpFromGround()
        {
            float jumpPower = PhysicsConsts.BaseJumpPower;
            if (jumpPower <= 1.0E-5f) return;

            DeltaMovement = new Vec3d(
                DeltaMovement.X,
                Math.Max(jumpPower, DeltaMovement.Y),
                DeltaMovement.Z);

            if (Sprinting)
            {
                float yawRad = Yaw * (MathF.PI / 180.0f);
                DeltaMovement = DeltaMovement.Add(
                    -MathF.Sin(yawRad) * PhysicsConsts.SprintJumpHorizontalBoost,
                    0,
                    MathF.Cos(yawRad) * PhysicsConsts.SprintJumpHorizontalBoost);
            }
        }

        private void Travel(World world, Vec3d input)
        {
            if (InWater && !CreativeFlying)
            {
                TravelInWater(world, input);
            }
            else if (InLava && !CreativeFlying)
            {
                TravelInLava(world, input);
            }
            else
            {
                TravelInAir(world, input);
            }
        }

        /// <summary>
        /// Ground/air travel — LivingEntity.travelInAir(Vec3)
        /// </summary>
        private void TravelInAir(World world, Vec3d input)
        {
            // Get block friction at feet
            float blockFriction = OnGround ? GetBlockFriction(world) : 1.0f;
            float f = blockFriction * PhysicsConsts.FrictionMultiplier;

            // Apply input → velocity (handleRelativeFrictionAndCalculateMovement)
            float speed = GetFrictionInfluencedSpeed(blockFriction);
            MoveRelative(speed, input);

            // Handle climbable
            HandleOnClimbable();

            // Execute collision
            Move(world, DeltaMovement);

            Vec3d postMoveVel = DeltaMovement;
            double vy = postMoveVel.Y;

            // Climbing wall bump
            if ((HorizontalCollision || Jumping) && OnClimbable)
            {
                vy = PhysicsConsts.ClimbWallBump;
            }

            // Apply gravity
            if (HasLevitation)
            {
                vy += (0.05 * (LevitationAmplifier + 1) - vy) * 0.2;
            }
            else
            {
                vy -= GetEffectiveGravity();
            }

            // Apply drag/friction
            if (CreativeFlying)
            {
                // Player.travel override: creative fly preserves horizontal from parent, damps Y
                DeltaMovement = new Vec3d(postMoveVel.X * f, vy * PhysicsConsts.FlyVerticalDamping, postMoveVel.Z * f);
            }
            else
            {
                DeltaMovement = new Vec3d(postMoveVel.X * f, vy * PhysicsConsts.DragY, postMoveVel.Z * f);
            }

            // Block speed factor (soul sand, honey, etc.)
            ApplyBlockSpeedFactor(world);
        }

        /// <summary>
        /// Water travel — LivingEntity.travelInWater(Vec3, ...)
        /// </summary>
        private void TravelInWater(World world, Vec3d input)
        {
            float slowDown = Sprinting ? PhysicsConsts.WaterSprintSlowDown : PhysicsConsts.WaterSlowDown;
            float speed = PhysicsConsts.WaterBaseSpeed;

            MoveRelative(speed, input);
            Move(world, DeltaMovement);

            Vec3d vel = DeltaMovement;

            // Climbing bump in water
            if (HorizontalCollision && OnClimbable)
                vel = new Vec3d(vel.X, PhysicsConsts.ClimbWallBump, vel.Z);

            vel = vel.Multiply(slowDown, PhysicsConsts.WaterYDamping, slowDown);

            // Gravity adjustment in water
            double gravity = GetEffectiveGravity();
            if (gravity != 0.0)
            {
                double adjustedY = vel.Y;
                bool falling = vel.Y <= 0.0;
                if (falling && Math.Abs(vel.Y - 0.005) >= PhysicsConsts.VerticalVelocityThreshold)
                {
                    adjustedY -= gravity / 16.0;
                }

                if (!OnGround)
                    adjustedY -= gravity / 16.0;

                vel = new Vec3d(vel.X, adjustedY, vel.Z);
            }

            DeltaMovement = vel;
        }

        /// <summary>
        /// Lava travel — LivingEntity.travelInLava(Vec3, ...)
        /// </summary>
        private void TravelInLava(World world, Vec3d input)
        {
            MoveRelative(PhysicsConsts.LavaSpeed, input);
            Move(world, DeltaMovement);

            double gravity = GetEffectiveGravity();
            Vec3d vel = DeltaMovement;
            vel = vel.Multiply(PhysicsConsts.LavaHorizontalDamping, PhysicsConsts.LavaVerticalDamping, PhysicsConsts.LavaHorizontalDamping);

            if (gravity != 0.0)
            {
                vel = vel.Add(0, -gravity / 4.0, 0);
            }

            DeltaMovement = vel;
        }

        /// <summary>
        /// Add input vector rotated by yaw to deltaMovement.
        /// Equivalent to Entity.moveRelative(float, Vec3) + getInputVector().
        /// </summary>
        private void MoveRelative(float speed, Vec3d input)
        {
            Vec3d rotated = GetInputVector(input, speed, Yaw);
            DeltaMovement = DeltaMovement.Add(rotated);
        }

        /// <summary>
        /// Rotate input by yaw and scale by speed. Equivalent to Entity.getInputVector().
        /// </summary>
        private static Vec3d GetInputVector(Vec3d input, float speed, float yaw)
        {
            double lenSqr = input.LengthSqr();
            if (lenSqr < 1.0E-7)
                return Vec3d.Zero;

            Vec3d scaled = (lenSqr > 1.0 ? input.Normalize() : input).Scale(speed);
            float sinYaw = MathF.Sin(yaw * (MathF.PI / 180.0f));
            float cosYaw = MathF.Cos(yaw * (MathF.PI / 180.0f));

            return new Vec3d(
                scaled.X * cosYaw - scaled.Z * sinYaw,
                scaled.Y,
                scaled.Z * cosYaw + scaled.X * sinYaw);
        }

        /// <summary>
        /// Execute movement with collision detection.
        /// Equivalent to Entity.move(MoverType.SELF, delta).
        /// </summary>
        private void Move(World world, Vec3d movement)
        {
            if (StuckSpeedMultiplier.LengthSqr() > 1.0E-7)
            {
                movement = movement.Multiply(StuckSpeedMultiplier);
                StuckSpeedMultiplier = Vec3d.Zero;
                DeltaMovement = Vec3d.Zero;
            }

            // Sneak edge back-off
            if (Sneaking && OnGround)
                movement = MaybeBackOffFromEdge(world, movement);

            Aabb box = GetBoundingBox();
            Vec3d resolved = CollisionDetector.Collide(world, box, movement, OnGround, PhysicsConsts.StepHeight);

            double resolvedLenSqr = resolved.LengthSqr();
            if (resolvedLenSqr > 1.0E-7 || movement.LengthSqr() - resolvedLenSqr < 1.0E-7)
            {
                // Fall distance reset via trace (simplified: reset on hitting ground)
                if (FallDistance != 0.0 && resolvedLenSqr >= 1.0)
                {
                    // Simplified: just check vertical collision
                }

                Position = Position.Add(resolved);
            }

            // Collision flags
            bool blockedX = !MthEqual(movement.X, resolved.X);
            bool blockedZ = !MthEqual(movement.Z, resolved.Z);
            HorizontalCollision = blockedX || blockedZ;
            VerticalCollision = movement.Y != resolved.Y;
            VerticalCollisionBelow = VerticalCollision && movement.Y < 0.0;
            OnGround = VerticalCollisionBelow;

            // Fall distance tracking
            if (OnGround)
                FallDistance = 0;
            else if (resolved.Y < 0)
                FallDistance -= resolved.Y;

            // Zero velocity on blocked axes
            if (HorizontalCollision)
            {
                DeltaMovement = new Vec3d(
                    blockedX ? 0 : DeltaMovement.X,
                    DeltaMovement.Y,
                    blockedZ ? 0 : DeltaMovement.Z);
            }

            if (VerticalCollision)
            {
                // Slime block bounce would go here; for now just zero Y
                DeltaMovement = new Vec3d(DeltaMovement.X, 0, DeltaMovement.Z);
            }
        }

        /// <summary>
        /// Sneak edge detection: prevent walking off edges while sneaking.
        /// Equivalent to Player.maybeBackOffFromEdge(Vec3, MoverType).
        /// </summary>
        private Vec3d MaybeBackOffFromEdge(World world, Vec3d movement)
        {
            if (movement.Y > 0) return movement;

            double step = 0.05;
            double dx = movement.X;
            double dz = movement.Z;
            Aabb box = GetBoundingBox();

            while (dx != 0.0 && CollisionDetector.CollectBlockColliders(world,
                box.Move(dx, -1.0, 0)).Count == 0)
            {
                dx = dx < step && dx >= -step ? 0.0 : (dx > 0.0 ? dx - step : dx + step);
            }

            while (dz != 0.0 && CollisionDetector.CollectBlockColliders(world,
                box.Move(0, -1.0, dz)).Count == 0)
            {
                dz = dz < step && dz >= -step ? 0.0 : (dz > 0.0 ? dz - step : dz + step);
            }

            while (dx != 0.0 && dz != 0.0 && CollisionDetector.CollectBlockColliders(world,
                box.Move(dx, -1.0, dz)).Count == 0)
            {
                dx = dx < step && dx >= -step ? 0.0 : (dx > 0.0 ? dx - step : dx + step);
                dz = dz < step && dz >= -step ? 0.0 : (dz > 0.0 ? dz - step : dz + step);
            }

            return new Vec3d(dx, movement.Y, dz);
        }

        /// <summary>
        /// Clamp velocity for climbable blocks.
        /// Equivalent to LivingEntity.handleOnClimbable(Vec3).
        /// </summary>
        private void HandleOnClimbable()
        {
            if (!OnClimbable) return;

            FallDistance = 0;
            double vx = Math.Clamp(DeltaMovement.X, -PhysicsConsts.ClimbMaxSpeed, PhysicsConsts.ClimbMaxSpeed);
            double vz = Math.Clamp(DeltaMovement.Z, -PhysicsConsts.ClimbMaxSpeed, PhysicsConsts.ClimbMaxSpeed);
            double vy = Math.Max(DeltaMovement.Y, -PhysicsConsts.ClimbMaxSpeed);

            // Sneaking on ladder prevents sliding down
            if (vy < 0.0 && Sneaking)
                vy = 0.0;

            DeltaMovement = new Vec3d(vx, vy, vz);
        }

        /// <summary>
        /// Get effective gravity considering slow falling effect.
        /// </summary>
        private double GetEffectiveGravity()
        {
            double gravity = PhysicsConsts.DefaultGravity;
            if (HasSlowFalling && DeltaMovement.Y <= 0.0)
                return Math.Min(gravity, PhysicsConsts.SlowFallingCap);
            return gravity;
        }

        /// <summary>
        /// Get speed based on friction: ground uses attribute speed * 0.216/(f^3), air uses 0.02.
        /// Equivalent to LivingEntity.getFrictionInfluencedSpeed(float).
        /// </summary>
        private float GetFrictionInfluencedSpeed(float friction)
        {
            if (OnGround)
            {
                return MovementSpeed * (PhysicsConsts.GroundAccelerationFactor / (friction * friction * friction));
            }
            else
            {
                return CreativeFlying ? MovementSpeed * 0.1f : PhysicsConsts.AirAcceleration;
            }
        }

        /// <summary>
        /// Get the friction of the block below the player's feet.
        /// </summary>
        private float GetBlockFriction(World world)
        {
            Location belowFeet = new(Position.X, Position.Y - 0.5000010, Position.Z);
            Material mat = world.GetBlock(belowFeet).Type;
            return GetMaterialFriction(mat);
        }

        /// <summary>
        /// Apply block speed factor (soul sand, honey, etc.)
        /// Equivalent to Entity.getBlockSpeedFactor().
        /// </summary>
        private void ApplyBlockSpeedFactor(World world)
        {
            Location atFeet = new(Position.X, Position.Y, Position.Z);
            Material mat = world.GetBlock(atFeet).Type;
            float factor = GetMaterialSpeedFactor(mat);

            if (factor == 1.0f)
            {
                Location belowFeet = new(Position.X, Position.Y - 0.5000010, Position.Z);
                mat = world.GetBlock(belowFeet).Type;
                factor = GetMaterialSpeedFactor(mat);
            }

            if (factor != 1.0f)
            {
                DeltaMovement = DeltaMovement.Multiply(factor, 1.0, factor);
            }
        }

        /// <summary>
        /// Get friction value for a material. Default 0.6, special blocks differ.
        /// </summary>
        public static float GetMaterialFriction(Material mat)
        {
            return mat switch
            {
                Material.Ice or Material.PackedIce => PhysicsConsts.IceFriction,
                Material.BlueIce => PhysicsConsts.BlueIceFriction,
                Material.SlimeBlock => PhysicsConsts.SlimeBlockFriction,
                Material.FrostedIce => PhysicsConsts.IceFriction,
                _ => PhysicsConsts.DefaultBlockFriction
            };
        }

        /// <summary>
        /// Get speed factor for a material.
        /// </summary>
        public static float GetMaterialSpeedFactor(Material mat)
        {
            return mat switch
            {
                Material.SoulSand or Material.SoulSoil => PhysicsConsts.SoulSandSpeedFactor,
                Material.HoneyBlock => PhysicsConsts.HoneySpeedFactor,
                _ => PhysicsConsts.DefaultSpeedFactor
            };
        }

        /// <summary>
        /// Update environmental state flags (in water, in lava, on climbable, etc.)
        /// Call before each Tick().
        /// </summary>
        public void UpdateEnvironment(World world)
        {
            Location feetLoc = new(Position.X, Position.Y, Position.Z);
            Location headLoc = new(Position.X, Position.Y + PlayerHeight * 0.5, Position.Z);

            Material feetBlock = world.GetBlock(feetLoc).Type;
            Material headBlock = world.GetBlock(headLoc).Type;

            InWater = feetBlock == Material.Water || headBlock == Material.Water
                      || feetBlock == Material.BubbleColumn;
            InLava = feetBlock == Material.Lava || headBlock == Material.Lava;
            OnClimbable = feetBlock.CanBeClimbedOn();
        }

        /// <summary>
        /// Set position from server teleport / initial spawn.
        /// </summary>
        public void SetPosition(double x, double y, double z)
        {
            Position = new Vec3d(x, y, z);
        }

        /// <summary>
        /// Set position and reset velocity (for teleports).
        /// </summary>
        public void Teleport(double x, double y, double z)
        {
            Position = new Vec3d(x, y, z);
            DeltaMovement = Vec3d.Zero;
            FallDistance = 0;
        }

        private static bool MthEqual(double a, double b)
        {
            return Math.Abs(a - b) < 1.0E-5;
        }
    }
}
