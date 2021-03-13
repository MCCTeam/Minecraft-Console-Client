using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Simple vector3 class
    /// </summary>
    public class Vector3
    {
        public double X;
        public double Y;
        public double Z;

        /// <summary>
        /// Create a new vector
        /// </summary>
        /// <param name="x">X value</param>
        /// <param name="y">Y value</param>
        /// <param name="z">Z value</param>
        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Add other vector and return new vector
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Vector3 Add(Vector3 other)
        {
            return new Vector3(X + other.X, Y + other.Y, Z + other.Z);
        }

        /// <summary>
        /// Multiply with other vector and return new vector
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        public Vector3 Multiply(double factor)
        {
            return new Vector3(X * factor, Y * factor, Z * factor);
        }

        public override string ToString()
        {
            return string.Format("X: {0} Y: {1} Z: {2}", X, Y, Z);
        }
    }
}