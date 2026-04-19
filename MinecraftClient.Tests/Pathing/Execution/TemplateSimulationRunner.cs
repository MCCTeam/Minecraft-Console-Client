using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class TemplateSimulationRunner
{
    internal static PlayerPhysics CreateGroundedPhysics(Location start, float yaw, int initialMomentumTicks = 0)
    {
        var physics = new PlayerPhysics
        {
            Position = new Vec3d(start.X, start.Y, start.Z),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = yaw,
            Pitch = 0f
        };

        if (initialMomentumTicks > 0)
            ApplyInitialGroundMomentum(physics, initialMomentumTicks);

        return physics;
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

    private static void ApplyInitialGroundMomentum(PlayerPhysics physics, int ticks)
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var seeded = new PlayerPhysics
        {
            Position = new Vec3d(0.5, 80, 0.5),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = physics.MovementSpeed,
            Yaw = physics.Yaw,
            Pitch = physics.Pitch
        };
        var input = new MovementInput
        {
            Forward = true,
            Sprint = true
        };

        for (int tick = 0; tick < ticks; tick++)
        {
            seeded.ApplyInput(input);
            seeded.Tick(world);
        }

        physics.DeltaMovement = seeded.DeltaMovement;
        physics.Sprinting = true;
    }
}
