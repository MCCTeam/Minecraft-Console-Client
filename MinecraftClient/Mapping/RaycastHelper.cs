using System;

namespace MinecraftClient.Mapping
{
    public static class RaycastHelper
    {
        public static Tuple<bool, Location, Block> RaycastBlock(McClient handler, double maxDistance, bool includeFluids)
        {
            return RaycastBlock(handler.GetWorld(), handler.GetCurrentLocation(), handler.GetYaw(), handler.GetPitch(), maxDistance, includeFluids);
        }

        public static Tuple<bool, Location, Block> RaycastBlock(World world, Location playerLocation, float yaw, float pitch, double maxDistance, bool includeFluids)
        {
            Location camera = playerLocation.EyesLocation();
            Location rotation = MathHelper.GetRotationVector(yaw, pitch);
            Location end = camera + (rotation * maxDistance);
            return Raycast(world, camera, end, includeFluids);
        }

        public static Tuple<bool, Location, Entity> RaycastEntity(McClient handler, double maxDistance)
        {
            throw new NotImplementedException();
        }

        private static Block CheckRaycastResult(World world, Location location, bool includeFluids)
        {
            Block block = world.GetBlock(location);

            if (!includeFluids && MaterialExtensions.IsLiquid(block.Type))
                return Block.Air;
            else
                return block;
        }

        public static Tuple<bool, Location, Block> Raycast(World world, Location start, Location end, bool includeFluids)
        {
            if (start == end)
                return new(false, Location.Zero, Block.Air);

            double start_x = MathHelper.Lerp(-1.0E-7, start.X, end.X);
            double start_y = MathHelper.Lerp(-1.0E-7, start.Y, end.Y);
            double start_z = MathHelper.Lerp(-1.0E-7, start.Z, end.Z);
            double end_x = MathHelper.Lerp(-1.0E-7, end.X, start.X);
            double end_y = MathHelper.Lerp(-1.0E-7, end.Y, start.Y);
            double end_z = MathHelper.Lerp(-1.0E-7, end.Z, start.Z);

            Location res_location = new(Math.Floor(start_x), Math.Floor(start_y), Math.Floor(start_z));
            Block block = CheckRaycastResult(world, res_location, includeFluids);
            if (block.Type != Material.Air)
                return new(true, res_location, block);

            double dx = end_x - start_x;
            double dy = end_y - start_y;
            double dz = end_z - start_z;
            int dx_sign = Math.Sign(dx);
            int dy_sign = Math.Sign(dy);
            int dz_sign = Math.Sign(dz);
            double x_step = dx_sign == 0 ? double.MaxValue : (double)dx_sign / dx;
            double y_step = dy_sign == 0 ? double.MaxValue : (double)dy_sign / dy;
            double z_step = dz_sign == 0 ? double.MaxValue : (double)dz_sign / dz;
            double x_frac = x_step * (dx_sign > 0 ? 1.0 - MathHelper.FractionalPart(start_x) : MathHelper.FractionalPart(start_x));
            double y_frac = y_step * (dy_sign > 0 ? 1.0 - MathHelper.FractionalPart(start_y) : MathHelper.FractionalPart(start_y));
            double z_frac = z_step * (dz_sign > 0 ? 1.0 - MathHelper.FractionalPart(start_z) : MathHelper.FractionalPart(start_z));

            while (x_frac <= 1.0 || y_frac <= 1.0 || z_frac <= 1.0)
            {
                if (x_frac < y_frac)
                {
                    if (x_frac < z_frac)
                    {
                        res_location.X += dx_sign;
                        x_frac += x_step;
                    }
                    else
                    {
                        res_location.Z += dz_sign;
                        z_frac += z_step;
                    }
                }
                else if (y_frac < z_frac)
                {
                    res_location.Y += dy_sign;
                    y_frac += y_step;
                }
                else
                {
                    res_location.Z += dz_sign;
                    z_frac += z_step;
                }

                block = CheckRaycastResult(world, res_location, includeFluids);
                if (block.Type != Material.Air)
                    return new(true, res_location, block);
            }

            return new(false, Location.Zero, Block.Air);
        }
    }

    public static class MathHelper
    {
        public static Location GetRotationVector(float yaw, float pitch)
        {
            float yaw_rad = -yaw * (MathF.PI / 180);
            (float yaw_sin, float yaw_cos) = MathF.SinCos(yaw_rad);

            float pitch_rad = pitch * (MathF.PI / 180);
            (float pitch_sin, float pitch_cos) = MathF.SinCos(pitch_rad);

            return new(yaw_sin * pitch_cos, -pitch_sin, yaw_cos * pitch_cos);
        }

        public static double Lerp(double delta, double start, double end)
        {
            return start + delta * (end - start);
        }

        public static long Lfloor(double value)
        {
            long l = (long)value;
            return value < (double)l ? l - 1L : l;
        }

        public static double FractionalPart(double value)
        {
            return value - Lfloor(value);
        }
    }
}
