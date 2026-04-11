using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    public enum TemplateState
    {
        InProgress,
        Complete,
        Failed
    }

    /// <summary>
    /// Per-tick movement controller for one path segment.
    /// Reads player state from physics, writes desired input to MovementInput,
    /// and reports completion or failure.
    /// </summary>
    public interface IActionTemplate
    {
        Location ExpectedStart { get; }
        Location ExpectedEnd { get; }

        TemplateState Tick(Location currentPos, PlayerPhysics physics, MovementInput input);
    }
}
