using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Goals;

namespace MinecraftClient.Tests.Pathing.Execution;

internal sealed record PathingExecutionScenario
{
    public required string Id { get; init; }
    public required Func<World> BuildWorld { get; init; }
    public required Location Start { get; init; }
    public required GoalBlock Goal { get; init; }
    public required float StartYaw { get; init; }
    public required int MaxExecutionTicks { get; init; }
}
