using System.Collections.Generic;

namespace MinecraftClient
{
    /// <summary>
    /// The type of an achievement or advancement.
    /// </summary>
    public enum AchievementType
    {
        Task,
        Challenge,
        Goal,
        Legacy
    }

    /// <summary>
    /// Represents a Minecraft achievement (pre-1.12) or advancement (1.12+).
    /// </summary>
    /// <param name="Id">Resource identifier, e.g. "minecraft:story/root" or "achievement.openInventory"</param>
    /// <param name="Title">Display title (null for legacy achievements without display info)</param>
    /// <param name="Description">Display description (null for legacy achievements without display info)</param>
    /// <param name="Type">The frame type / achievement category</param>
    /// <param name="IsHidden">Whether this advancement is hidden in the UI</param>
    /// <param name="IsCompleted">Whether all requirements have been met</param>
    /// <param name="Requirements">OR-groups of criterion names; all groups must be satisfied</param>
    /// <param name="CriteriaProgress">Per-criterion completion status</param>
    public record Achievement(
        string Id,
        string? Title,
        string? Description,
        AchievementType Type,
        bool IsHidden,
        bool IsCompleted,
        IReadOnlyList<IReadOnlyList<string>> Requirements,
        IReadOnlyDictionary<string, bool> CriteriaProgress);
}
