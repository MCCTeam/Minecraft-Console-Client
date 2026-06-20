using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Axis-aligned bounding box, mirrors net.minecraft.world.phys.AABB.
    /// Immutable — mutating methods return new instances.
    /// </summary>
    public readonly struct Aabb : IEquatable<Aabb>
    {
        public static readonly Aabb Empty = new(0, 0, 0, 0, 0, 0);

        public readonly double MinX, MinY, MinZ;
        public readonly double MaxX, MaxY, MaxZ;

        public Aabb(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            MinX = Math.Min(x1, x2);
            MinY = Math.Min(y1, y2);
            MinZ = Math.Min(z1, z2);
            MaxX = Math.Max(x1, x2);
            MaxY = Math.Max(y1, y2);
            MaxZ = Math.Max(z1, z2);
        }

        /// <summary>
        /// Create a player-style AABB centered on feetX/Z with given width and height
        /// </summary>
        public static Aabb OfSize(double centerX, double feetY, double centerZ, double width, double height)
        {
            double hw = width / 2.0;
            return new Aabb(centerX - hw, feetY, centerZ - hw, centerX + hw, feetY + height, centerZ + hw);
        }

        /// <summary>
        /// Full block AABB at given integer position
        /// </summary>
        public static Aabb BlockAt(int x, int y, int z) =>
            new(x, y, z, x + 1.0, y + 1.0, z + 1.0);

        public double XSize => MaxX - MinX;
        public double YSize => MaxY - MinY;
        public double ZSize => MaxZ - MinZ;

        public double Min(int axis) => axis switch { 0 => MinX, 1 => MinY, _ => MinZ };
        public double Max(int axis) => axis switch { 0 => MaxX, 1 => MaxY, _ => MaxZ };

        /// <summary>
        /// Expand toward a movement direction (vanilla expandTowards)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb ExpandTowards(double dx, double dy, double dz)
        {
            double minX = MinX, minY = MinY, minZ = MinZ;
            double maxX = MaxX, maxY = MaxY, maxZ = MaxZ;
            if (dx < 0) minX += dx; else if (dx > 0) maxX += dx;
            if (dy < 0) minY += dy; else if (dy > 0) maxY += dy;
            if (dz < 0) minZ += dz; else if (dz > 0) maxZ += dz;
            return new Aabb(minX, minY, minZ, maxX, maxY, maxZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb ExpandTowards(Vec3d v) => ExpandTowards(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb Inflate(double x, double y, double z) =>
            new(MinX - x, MinY - y, MinZ - z, MaxX + x, MaxY + y, MaxZ + z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb Inflate(double v) => Inflate(v, v, v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb Deflate(double x, double y, double z) => Inflate(-x, -y, -z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb Move(double dx, double dy, double dz) =>
            new(MinX + dx, MinY + dy, MinZ + dz, MaxX + dx, MaxY + dy, MaxZ + dz);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Aabb Move(Vec3d v) => Move(v.X, v.Y, v.Z);

        /// <summary>
        /// Strict overlap test (vanilla uses &lt; and &gt;, not &lt;=)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(Aabb other) =>
            MinX < other.MaxX && MaxX > other.MinX &&
            MinY < other.MaxY && MaxY > other.MinY &&
            MinZ < other.MaxZ && MaxZ > other.MinZ;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Intersects(double x1, double y1, double z1, double x2, double y2, double z2) =>
            MinX < x2 && MaxX > x1 && MinY < y2 && MaxY > y1 && MinZ < z2 && MaxZ > z1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(double x, double y, double z) =>
            x >= MinX && x < MaxX && y >= MinY && y < MaxY && z >= MinZ && z < MaxZ;

        /// <summary>
        /// Collide this AABB along a single axis against another AABB.
        /// Returns the clamped movement distance.
        /// </summary>
        /// <summary>
        /// Clip entity movement along X against a block shape (other).
        /// Vanilla semantics: VoxelShape.collide(Axis.X, entityBox, movement).
        /// </summary>
        public double CollideX(Aabb other, double movement)
        {
            if (other.MaxY <= MinY || other.MinY >= MaxY || other.MaxZ <= MinZ || other.MinZ >= MaxZ)
                return movement;
            if (movement > 0.0 && other.MinX >= MaxX)
            {
                double d = other.MinX - MaxX;
                if (d < movement) movement = d;
            }
            else if (movement < 0.0 && other.MaxX <= MinX)
            {
                double d = other.MaxX - MinX;
                if (d > movement) movement = d;
            }
            return movement;
        }

        public double CollideY(Aabb other, double movement)
        {
            if (other.MaxX <= MinX || other.MinX >= MaxX || other.MaxZ <= MinZ || other.MinZ >= MaxZ)
                return movement;
            if (movement > 0.0 && other.MinY >= MaxY)
            {
                double d = other.MinY - MaxY;
                if (d < movement) movement = d;
            }
            else if (movement < 0.0 && other.MaxY <= MinY)
            {
                double d = other.MaxY - MinY;
                if (d > movement) movement = d;
            }
            return movement;
        }

        public double CollideZ(Aabb other, double movement)
        {
            if (other.MaxX <= MinX || other.MinX >= MaxX || other.MaxY <= MinY || other.MinY >= MaxY)
                return movement;
            if (movement > 0.0 && other.MinZ >= MaxZ)
            {
                double d = other.MinZ - MaxZ;
                if (d < movement) movement = d;
            }
            else if (movement < 0.0 && other.MaxZ <= MinZ)
            {
                double d = other.MaxZ - MinZ;
                if (d > movement) movement = d;
            }
            return movement;
        }

        /// <summary>
        /// Collide along an axis (0=X, 1=Y, 2=Z) against another AABB
        /// </summary>
        public double Collide(int axis, Aabb other, double movement)
        {
            return axis switch
            {
                0 => CollideX(other, movement),
                1 => CollideY(other, movement),
                2 => CollideZ(other, movement),
                _ => movement
            };
        }

        public Vec3d GetCenter() => new(
            (MinX + MaxX) * 0.5,
            (MinY + MaxY) * 0.5,
            (MinZ + MaxZ) * 0.5);

        public Vec3d GetBottomCenter() => new(
            (MinX + MaxX) * 0.5,
            MinY,
            (MinZ + MaxZ) * 0.5);

        public bool Equals(Aabb other) =>
            MinX == other.MinX && MinY == other.MinY && MinZ == other.MinZ &&
            MaxX == other.MaxX && MaxY == other.MaxY && MaxZ == other.MaxZ;

        public override bool Equals(object? obj) => obj is Aabb a && Equals(a);
        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MinZ, MaxX, MaxY, MaxZ);
        public override string ToString() => $"AABB[{MinX:F3},{MinY:F3},{MinZ:F3} -> {MaxX:F3},{MaxY:F3},{MaxZ:F3}]";

        public static bool operator ==(Aabb a, Aabb b) => a.Equals(b);
        public static bool operator !=(Aabb a, Aabb b) => !a.Equals(b);
    }
}
