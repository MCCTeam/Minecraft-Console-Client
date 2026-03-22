using System;
using System.Runtime.CompilerServices;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Immutable 3D double vector, mirrors net.minecraft.world.phys.Vec3
    /// </summary>
    public readonly struct Vec3d : IEquatable<Vec3d>
    {
        public static readonly Vec3d Zero = new(0, 0, 0);

        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d Add(double x, double y, double z) => new(X + x, Y + y, Z + z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d Add(Vec3d other) => new(X + other.X, Y + other.Y, Z + other.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d Subtract(Vec3d other) => new(X - other.X, Y - other.Y, Z - other.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d Subtract(double x, double y, double z) => new(X - x, Y - y, Z - z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d Scale(double factor) => new(X * factor, Y * factor, Z * factor);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d Multiply(double x, double y, double z) => new(X * x, Y * y, Z * z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3d Multiply(Vec3d other) => new(X * other.X, Y * other.Y, Z * other.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthSqr() => X * X + Y * Y + Z * Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(LengthSqr());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double HorizontalDistanceSqr() => X * X + Z * Z;

        public Vec3d Normalize()
        {
            double len = Length();
            return len < 1.0E-7 ? Zero : new Vec3d(X / len, Y / len, Z / len);
        }

        /// <summary>
        /// Get component by axis index: 0=X, 1=Y, 2=Z
        /// </summary>
        public double Get(int axis) => axis switch
        {
            0 => X,
            1 => Y,
            2 => Z,
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };

        /// <summary>
        /// Return a new Vec3d with one axis replaced
        /// </summary>
        public Vec3d With(int axis, double value) => axis switch
        {
            0 => new Vec3d(value, Y, Z),
            1 => new Vec3d(X, value, Z),
            2 => new Vec3d(X, Y, value),
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };

        public bool Equals(Vec3d other) =>
            X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object? obj) =>
            obj is Vec3d other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(X, Y, Z);

        public override string ToString() =>
            $"({X:F4}, {Y:F4}, {Z:F4})";

        public static bool operator ==(Vec3d a, Vec3d b) => a.Equals(b);
        public static bool operator !=(Vec3d a, Vec3d b) => !a.Equals(b);
        public static Vec3d operator +(Vec3d a, Vec3d b) => a.Add(b);
        public static Vec3d operator -(Vec3d a, Vec3d b) => a.Subtract(b);
        public static Vec3d operator *(Vec3d a, double s) => a.Scale(s);
        public static Vec3d operator -(Vec3d a) => new(-a.X, -a.Y, -a.Z);
    }
}
