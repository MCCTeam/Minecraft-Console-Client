using System;

namespace MinecraftClient.Physics
{
    /// <summary>
    /// Represents movement input state, equivalent to vanilla ClientInput / KeyboardInput.
    /// </summary>
    public class MovementInput
    {
        public bool Forward;
        public bool Back;
        public bool Left;
        public bool Right;
        public bool Jump;
        public bool Sneak;
        public bool Sprint;

        /// <summary>
        /// Get the raw input vector (xxa, zza) before rotation.
        /// Forward = +zza, Back = -zza, Left = +xxa, Right = -xxa.
        /// Then normalized if magnitude > 1.
        /// </summary>
        public (float xxa, float zza) GetMoveVector()
        {
            float xxa = 0;
            float zza = 0;

            if (Forward) zza += 1.0f;
            if (Back) zza -= 1.0f;
            if (Left) xxa += 1.0f;
            if (Right) xxa -= 1.0f;

            float lenSqr = xxa * xxa + zza * zza;
            if (lenSqr > 1.0f)
            {
                float len = MathF.Sqrt(lenSqr);
                xxa /= len;
                zza /= len;
            }

            return (xxa, zza);
        }

        public void Reset()
        {
            Forward = false;
            Back = false;
            Left = false;
            Right = false;
            Jump = false;
            Sneak = false;
            Sprint = false;
        }
    }
}
