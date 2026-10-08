using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using System.Globalization;
using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using DMCBK.Core;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>
/// Builds the TUI status bar the way the legacy client drew it (MinecraftClient/Tui/MainTuiView.cs:921-1070): heart glyphs and the health value, food glyphs and the food value, XP progress and level, then the active effects with a per-effect icon, colour and remaining duration.
/// <para>
/// The new client rendered a flat "HP 20 Food 20 Survival XYZ 122 64 330" instead, which is a different bar entirely: no glyphs, no effects, and nothing to see at a glance.
/// </para>
/// </summary>
internal static class StatusBarBuilder
{
    private const int Segments = 10;
    private const string HeartFilled = "❤️";
    private const string HeartEmpty = " ♡ ";
    private const string FoodFilled = "🍖";
    private const string FoodEmpty = " ○ ";
    private const string ExperienceFilled = "█";
    private const string ExperienceEmpty = "░";

    private static readonly IBrush HeartBrush = new SolidColorBrush(Color.FromRgb(255, 85, 85));
    private static readonly IBrush HealthValueBrush = new SolidColorBrush(Color.FromRgb(255, 150, 150));
    private static readonly IBrush FoodBrush = new SolidColorBrush(Color.FromRgb(200, 160, 80));
    private static readonly IBrush FoodValueBrush = new SolidColorBrush(Color.FromRgb(220, 190, 100));
    private static readonly IBrush ExperienceBrush = new SolidColorBrush(Color.FromRgb(85, 255, 85));
    private static readonly IBrush ExperienceValueBrush = new SolidColorBrush(Color.FromRgb(170, 255, 120));
    private static readonly IBrush SeparatorBrush = new SolidColorBrush(Color.FromRgb(170, 170, 170));

    /// <summary>
    /// Appends the bar's runs.
    /// <paramref name="showEffectNames"/> is legacy's <c>Advanced.ShowEffectNamesInTUI</c>: on, the effect's full name; off, its compact icon.
    /// </summary>
    public static void Build(
        InlineCollection inlines,
        float health,
        int food,
        int experienceLevel,
        float experienceProgress,
        int totalExperience,
        IReadOnlyList<EffectSnapshot> effects,
        bool showEffectNames,
        Umpk.Text.ITranslationSource translations)
    {
        ArgumentNullException.ThrowIfNull(inlines);
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(translations);

        inlines.Clear();

        int hearts = Math.Clamp((int)Math.Ceiling(health / 20f * Segments), 0, Segments);
        int bites = Math.Clamp((int)Math.Ceiling(food / 20f * Segments), 0, Segments);
        int experience = Math.Clamp((int)Math.Round(experienceProgress * Segments), 0, Segments);

        inlines.Add(new Run(Bar(hearts, HeartFilled, HeartEmpty)) { Foreground = HeartBrush });
        inlines.Add(new Run(FormattableString.Invariant($" {health:F1}  ")) { Foreground = HealthValueBrush });
        inlines.Add(new Run(Bar(bites, FoodFilled, FoodEmpty)) { Foreground = FoodBrush });
        inlines.Add(new Run($" {food.ToString(CultureInfo.CurrentCulture)}") { Foreground = FoodValueBrush });
        inlines.Add(new Run("  "));
        inlines.Add(new Run(ProgressBar(experience)) { Foreground = ExperienceBrush });
        inlines.Add(new Run(Strings.TuiExperienceValue(experienceLevel, totalExperience))
        {
            Foreground = ExperienceValueBrush,
        });

        if (effects.Count == 0)
            return;

        inlines.Add(new Run("  |  ") { Foreground = SeparatorBrush });

        bool first = true;
        foreach (EffectSnapshot effect in effects)
        {
            if (!first)
                inlines.Add(new Run(", ") { Foreground = SeparatorBrush });

            first = false;
            (string icon, IBrush colour) = IconAndColour(effect.EffectId);
            string label = showEffectNames ? EffectText.DisplayName(effect, translations) : Compact(icon, effect);
            inlines.Add(new Run($"{label} ({EffectText.ShortDuration(effect)})") { Foreground = colour });
        }
    }

    // Legacy BuildBarText (MainTuiView.cs:1009-1022): filled glyphs are space separated, empty ones carry their own padding, which is what keeps both bars the same width at every value.
    private static string Bar(int filled, string filledGlyph, string emptyGlyph)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < filled; i++)
        {
            if (i > 0)
                sb.Append(' ');

            sb.Append(filledGlyph);
        }

        for (int i = filled; i < Segments; i++)
            sb.Append(emptyGlyph);

        return sb.ToString();
    }

    private static string ProgressBar(int filled)
        => "[" + string.Concat(Enumerable.Repeat(ExperienceFilled, filled))
            + string.Concat(Enumerable.Repeat(ExperienceEmpty, Segments - filled)) + "]";

    private static string Compact(string icon, EffectSnapshot effect)
        => effect.Level > 1 ? $"{icon}{effect.Level}" : icon;

    /// <summary>
    /// The per-effect icon and colour, ported from legacy's GetEffectIconAndColor (MinecraftClient/Tui/MainTuiView.cs:1032-1070).
    /// Legacy switched on its own Effects enum; the new client carries the vanilla registry id, so the match is on the id's path.
    /// </summary>
    private static (string Icon, IBrush Colour) IconAndColour(string effectId)
    {
        string path = effectId;
        int colon = path.IndexOf(':', StringComparison.Ordinal);
        if (colon >= 0)
            path = path[(colon + 1)..];

        return path switch
        {
            "speed" => ("⚡", Rgb(135, 206, 235)),
            "slowness" => ("🐢", Rgb(139, 139, 139)),
            "haste" => ("⛏", Rgb(255, 215, 0)),
            "mining_fatigue" => ("🔨", Rgb(64, 64, 64)),
            "strength" => ("⚔", Rgb(255, 99, 71)),
            "instant_health" => ("❤", Rgb(255, 182, 193)),
            "instant_damage" => ("💀", Rgb(139, 0, 0)),
            "jump_boost" => ("🦘", Rgb(50, 205, 50)),
            "nausea" => ("💫", Rgb(85, 107, 47)),
            "regeneration" => ("✨", Rgb(255, 105, 180)),
            "resistance" => ("🛡", Rgb(112, 128, 144)),
            "fire_resistance" => ("🔥", Rgb(255, 140, 0)),
            "water_breathing" => ("🐟", Rgb(0, 191, 255)),
            "invisibility" => ("👻", Rgb(200, 200, 200)),
            "blindness" => ("🕶", Rgb(50, 50, 50)),
            "night_vision" => ("👁", Rgb(0, 255, 127)),
            "hunger" => ("🍔", Rgb(139, 69, 19)),
            "weakness" => ("💪", Rgb(128, 128, 128)),
            "poison" => ("☠", Rgb(75, 0, 130)),
            "wither" => ("🥀", Rgb(0, 0, 0)),
            "health_boost" => ("💖", Rgb(255, 20, 147)),
            "absorption" => ("💛", Rgb(255, 215, 0)),
            "saturation" => ("🍖", Rgb(255, 165, 0)),
            "glowing" => ("💡", Rgb(255, 255, 150)),
            "levitation" => ("🎈", Rgb(147, 112, 219)),
            "luck" => ("🍀", Rgb(50, 205, 50)),
            "unluck" => ("🐈‍⬛", Rgb(128, 0, 0)),
            "slow_falling" => ("🪶", Rgb(255, 182, 193)),
            "conduit_power" => ("🐡", Rgb(0, 255, 255)),
            "dolphins_grace" => ("🐬", Rgb(135, 206, 235)),
            "bad_omen" => ("🏴", Rgb(0, 100, 0)),
            "hero_of_the_village" => ("🎉", Rgb(255, 215, 0)),
            _ => ("✦", Rgb(200, 200, 200)),
        };
    }

    private static SolidColorBrush Rgb(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));
}
