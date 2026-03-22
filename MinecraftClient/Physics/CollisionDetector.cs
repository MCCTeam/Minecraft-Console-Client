using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Performs AABB collision detection against the block world.
    /// Mirrors Entity.collide(), collideBoundingBox(), collideWithShapes() from vanilla MC.
    /// </summary>
    public static class CollisionDetector
    {
        /// <summary>
        /// Resolve movement with full collision detection including step-up.
        /// This is the main entry point, equivalent to Entity.collide(Vec3).
        /// </summary>
        public static Vec3d Collide(World world, Aabb entityBox, Vec3d movement, bool onGround, float maxUpStep)
        {
            if (movement.LengthSqr() == 0.0)
                return movement;

            // Collect block collision shapes in the movement path
            var colliders = CollectBlockColliders(world, entityBox.ExpandTowards(movement));
            Vec3d resolved = CollideWithShapes(movement, entityBox, colliders);

            bool blockedX = movement.X != resolved.X;
            bool blockedZ = movement.Z != resolved.Z;
            bool blockedY = movement.Y != resolved.Y;
            bool hitGroundDuringMove = blockedY && movement.Y < 0.0;

            // Step-up logic: if blocked horizontally and on ground or just landed
            if (maxUpStep > 0.0f && (hitGroundDuringMove || onGround) && (blockedX || blockedZ))
            {
                // Try stepping up
                Aabb stepBase = hitGroundDuringMove ? entityBox.Move(0, resolved.Y, 0) : entityBox;
                Aabb expanded = stepBase.ExpandTowards(movement.X, maxUpStep, movement.Z)
                    .ExpandTowards(0, hitGroundDuringMove ? 0 : -1.0E-5, 0);

                var stepColliders = CollectBlockColliders(world, expanded);

                // Try various step heights
                float[] candidateHeights = CollectCandidateStepHeights(stepBase, stepColliders, maxUpStep, (float)resolved.Y);

                foreach (float stepY in candidateHeights)
                {
                    Vec3d stepMovement = new Vec3d(movement.X, stepY, movement.Z);
                    Vec3d stepResolved = CollideWithShapes(stepMovement, stepBase, stepColliders);

                    if (stepResolved.HorizontalDistanceSqr() > resolved.HorizontalDistanceSqr())
                    {
                        double yOffset = entityBox.MinY - stepBase.MinY;
                        return stepResolved.Subtract(0, yOffset, 0);
                    }
                }
            }

            return resolved;
        }

        /// <summary>
        /// Collide movement against a list of shapes using axis-separated resolution.
        /// Matches Entity.collideWithShapes() — processes axes in order of smallest movement first.
        /// </summary>
        private static Vec3d CollideWithShapes(Vec3d movement, Aabb entityBox, List<Aabb> colliders)
        {
            if (colliders.Count == 0)
                return movement;

            Vec3d accumulated = Vec3d.Zero;
            int[] axisOrder = GetAxisStepOrder(movement);

            foreach (int axis in axisOrder)
            {
                double dist = movement.Get(axis);
                if (dist == 0.0) continue;

                double resolved = CollideAxis(axis, entityBox.Move(accumulated), colliders, dist);
                accumulated = accumulated.With(axis, resolved);
            }

            return accumulated;
        }

        /// <summary>
        /// Get axis processing order: Y first if moving down, otherwise smallest absolute movement first.
        /// Vanilla uses Direction.axisStepOrder(Vec3) which returns axes sorted by absolute movement.
        /// </summary>
        private static int[] GetAxisStepOrder(Vec3d movement)
        {
            double absX = Math.Abs(movement.X);
            double absY = Math.Abs(movement.Y);
            double absZ = Math.Abs(movement.Z);

            if (absX > absZ)
            {
                if (absZ > absY)
                    return new[] { 1, 2, 0 }; // Y Z X
                if (absX > absY)
                    return new[] { 1, 0, 2 }; // Y X Z
                return new[] { 0, 1, 2 }; // X Y Z
            }
            else
            {
                if (absX > absY)
                    return new[] { 1, 0, 2 }; // Y X Z
                if (absZ > absY)
                    return new[] { 1, 2, 0 }; // Y Z X
                return new[] { 2, 1, 0 }; // Z Y X
            }
        }

        /// <summary>
        /// Collide along a single axis against all block shapes.
        /// Equivalent to Shapes.collide(axis, box, shapes, distance).
        /// </summary>
        private static double CollideAxis(int axis, Aabb entityBox, List<Aabb> colliders, double movement)
        {
            foreach (var collider in colliders)
            {
                if (Math.Abs(movement) < PhysicsConsts.CollisionEpsilon)
                    return 0.0;
                movement = entityBox.Collide(axis, collider, movement);
            }
            return movement;
        }

        /// <summary>
        /// Collect all block collision AABBs that overlap the given search area.
        /// Equivalent to BlockCollisions iterator in vanilla.
        /// </summary>
        public static List<Aabb> CollectBlockColliders(World world, Aabb searchBox)
        {
            var result = new List<Aabb>();

            int minBX = (int)Math.Floor(searchBox.MinX - PhysicsConsts.CollisionEpsilon) - 1;
            int maxBX = (int)Math.Floor(searchBox.MaxX + PhysicsConsts.CollisionEpsilon) + 1;
            int minBY = (int)Math.Floor(searchBox.MinY - PhysicsConsts.CollisionEpsilon) - 1;
            int maxBY = (int)Math.Floor(searchBox.MaxY + PhysicsConsts.CollisionEpsilon) + 1;
            int minBZ = (int)Math.Floor(searchBox.MinZ - PhysicsConsts.CollisionEpsilon) - 1;
            int maxBZ = (int)Math.Floor(searchBox.MaxZ + PhysicsConsts.CollisionEpsilon) + 1;

            for (int bx = minBX; bx <= maxBX; bx++)
            {
                for (int bz = minBZ; bz <= maxBZ; bz++)
                {
                    for (int by = minBY; by <= maxBY; by++)
                    {
                        Block block = world.GetBlock(new Location(bx, by, bz));
                        Aabb[] shapes = BlockShapes.GetShapes(block);

                        foreach (var shape in shapes)
                        {
                            Aabb worldShape = shape.Move(bx, by, bz);
                            if (worldShape.Intersects(searchBox))
                                result.Add(worldShape);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Collect candidate step-up heights, matching Entity.collectCandidateStepUpHeights().
        /// Returns sorted distinct step heights between current resolved Y and maxUpStep.
        /// </summary>
        private static float[] CollectCandidateStepHeights(Aabb stepBase, List<Aabb> colliders, float maxUpStep, float currentY)
        {
            var heights = new SortedSet<float>();

            foreach (var collider in colliders)
            {
                float h = (float)(collider.MaxY - stepBase.MinY);
                if (h > currentY && h <= maxUpStep)
                    heights.Add(h);
            }

            if (heights.Count == 0)
                return new[] { maxUpStep };

            var result = new float[heights.Count];
            heights.CopyTo(result);
            return result;
        }

        /// <summary>
        /// Check if a position is on ground by testing for vertical collision below.
        /// </summary>
        public static bool IsOnGround(World world, Aabb entityBox)
        {
            Aabb testBox = entityBox.ExpandTowards(0, -0.06, 0);
            return CollectBlockColliders(world, testBox).Count > 0;
        }

        /// <summary>
        /// Check if a given position has no collision (for checking if player fits somewhere).
        /// </summary>
        public static bool NoCollision(World world, Aabb entityBox)
        {
            return CollectBlockColliders(world, entityBox).Count == 0;
        }
    }
}
