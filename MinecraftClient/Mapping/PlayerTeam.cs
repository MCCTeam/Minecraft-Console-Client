using System.Collections.Generic;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft scoreboard team and its current state.
    /// </summary>
    public class PlayerTeam
    {
        /// <summary>Team internal name (up to 16 chars)</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Display name component (formatted text)</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Friendly fire is allowed between team members</summary>
        public bool AllowFriendlyFire { get; set; }

        /// <summary>Team members can see invisible teammates</summary>
        public bool SeeFriendlyInvisibles { get; set; }

        /// <summary>
        /// Nametag visibility rule.
        /// Values: "always", "hideForOtherTeams", "hideForOwnTeam", "never"
        /// </summary>
        public string NameTagVisibility { get; set; } = string.Empty;

        /// <summary>
        /// Collision rule.
        /// Values: "always", "pushOtherTeams", "pushOwnTeam", "never"
        /// </summary>
        public string CollisionRule { get; set; } = string.Empty;

        /// <summary>
        /// Team color as ChatFormatting enum ordinal (-1 = RESET/none,
        /// 0–15 = BLACK … WHITE).
        /// </summary>
        public int Color { get; set; } = -1;

        /// <summary>Prefix displayed before member names (formatted text)</summary>
        public string Prefix { get; set; } = string.Empty;

        /// <summary>Suffix displayed after member names (formatted text)</summary>
        public string Suffix { get; set; } = string.Empty;

        /// <summary>Current set of player / entity names on this team</summary>
        public HashSet<string> Members { get; } = new(System.StringComparer.OrdinalIgnoreCase);
    }
}
