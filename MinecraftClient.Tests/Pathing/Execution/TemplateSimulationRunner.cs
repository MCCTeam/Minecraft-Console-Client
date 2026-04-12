using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class TemplateSimulationRunner
{
    internal static PlayerPhysics CreateGroundedPhysics(Location start, float yaw)
    {
        return new PlayerPhysics
        {
            Position = new Vec3d(start.X, start.Y, start.Z),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = yaw,
            Pitch = 0f
        };
    }

    internal static TemplateState Run(IActionTemplate template, PlayerPhysics physics, World world, int maxTicks, out Location finalPos)
    {
        var input = new MovementInput();
        TemplateState state = TemplateState.InProgress;

        for (int tick = 0; tick < maxTicks; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            state = template.Tick(pos, physics, input, world);
            if (state != TemplateState.InProgress)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        finalPos = new Location(physics.Position.X, physics.Position.Y, physics.Position.Z);
        return state;
    }
}
