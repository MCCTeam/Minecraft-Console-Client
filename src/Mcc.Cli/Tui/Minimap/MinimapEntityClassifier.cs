using Mcc.Cli.Localization;
using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Minimap;

/// <summary>
/// Classifies entities into minimap categories from the embedded <c>MinimapEntityCategories.json</c> (ported from the legacy tree; keyed by MCC <c>EntityType</c> enum names, matched via <see cref="RegistryKey.ToPascal"/> against the core's namespaced type ids).
/// Players are handled specially (a non-null <c>PlayerName</c> on the snapshot).
/// Mirrors the legacy <c>MinimapEntityClassifier</c>.
/// Pure and unit-testable.
/// </summary>
internal static class MinimapEntityClassifier
{
    public static readonly Color HostileColor = Color.FromRgb(220, 60, 60);
    public static readonly Color PassiveColor = Color.FromRgb(90, 200, 90);
    public static readonly Color NeutralColor = Color.FromRgb(220, 200, 80);
    public static readonly Color PlayerColor = Color.FromRgb(240, 240, 255);
    public static readonly Color NonLivingColor = Color.FromRgb(150, 150, 150);

    /// <summary>The depth-faded color entities below the player fade toward, ported from legacy's <c>FadedGray</c>.</summary>
    public static readonly Color FadedColor = Color.FromRgb(100, 100, 100);

    private static readonly FrozenDictionary<string, MobCategory> Categories;

    static MinimapEntityClassifier()
    {
        var map = new Dictionary<string, MobCategory>(StringComparer.Ordinal);
        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Mcc.Cli.Tui.Minimap.MinimapEntityCategories.json");
            if (stream is not null)
            {
                using JsonDocument document = JsonDocument.Parse(stream);
                JsonElement root = document.RootElement;
                Load(root, "hostile", MobCategory.Hostile, map);
                Load(root, "passive", MobCategory.Passive, map);
                Load(root, "neutral", MobCategory.Neutral, map);
                Load(root, "non_living", MobCategory.NonLiving, map);
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Absent/corrupt resource: everything falls through to NonLiving.
        }

        Categories = map.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>The number of classified entity types (for diagnostics/tests).</summary>
    public static int Count => Categories.Count;

    /// <summary>Classifies an entity by its namespaced type id, treating named players as <see cref="MobCategory.Player"/>.</summary>
    public static MobCategory Classify(string typeId, bool isPlayer)
    {
        if (isPlayer)
            return MobCategory.Player;

        return Categories.TryGetValue(RegistryKey.ToPascal(typeId), out MobCategory category)
            ? category
            : MobCategory.NonLiving;
    }

    /// <summary>The base color for a category.</summary>
    public static Color GetColor(MobCategory category) => category switch
    {
        MobCategory.Hostile => HostileColor,
        MobCategory.Passive => PassiveColor,
        MobCategory.Neutral => NeutralColor,
        MobCategory.Player => PlayerColor,
        _ => NonLivingColor,
    };

    /// <summary>Draw priority (higher wins when two entities share a cell): hostile &gt; player &gt; neutral &gt; passive &gt; non-living.</summary>
    public static int GetPriority(MobCategory category) => category switch
    {
        MobCategory.Hostile => 4,
        MobCategory.Player => 3,
        MobCategory.Neutral => 2,
        MobCategory.Passive => 1,
        _ => 0,
    };

    /// <summary>
    /// Fades a category color toward <see cref="FadedColor"/> as an entity sits deeper below the player , ported from legacy's <c>ApplyDepthFade</c>: unfaded within 5 blocks below, fully faded at 15 or more, linear in between.
    /// Entities at or above the player's Y are never faded.
    /// </summary>
    public static Color ApplyDepthFade(Color baseColor, double playerY, double entityY)
    {
        double depth = playerY - entityY;
        if (depth <= 5.0)
            return baseColor;

        if (depth >= 15.0)
            return FadedColor;

        double t = (depth - 5.0) / 10.0;
        return Lerp(baseColor, FadedColor, t);
    }

    /// <summary>
    /// Whether an entity this deep below the player should be shown at all, ported from legacy's <c>ShouldDisplay</c>: players always show; other entities more than 15 blocks below the player are dropped instead of just faded, so the minimap does not clutter with mobs several floors down.
    /// </summary>
    public static bool ShouldDisplay(MobCategory category, double playerY, double entityY)
    {
        if (category == MobCategory.Player)
            return true;

        if (entityY >= playerY)
            return true;

        return playerY - entityY <= 15.0;
    }

    /// <summary>A short legend label for a category, ported from legacy's <c>GetCategoryLabel</c>.</summary>
    public static string GetCategoryLabel(MobCategory category) => category switch
    {
        MobCategory.Hostile => Strings.MinimapLegendHostile,
        MobCategory.Passive => Strings.MinimapLegendPassive,
        MobCategory.Neutral => Strings.MinimapLegendNeutral,
        MobCategory.Player => Strings.MinimapLegendPlayer,
        _ => "?",
    };

    private static Color Lerp(Color a, Color b, double t)
        => Color.FromRgb(
            (byte)(a.R + ((b.R - a.R) * t)),
            (byte)(a.G + ((b.G - a.G) * t)),
            (byte)(a.B + ((b.B - a.B) * t)));

    private static void Load(JsonElement root, string name, MobCategory category, Dictionary<string, MobCategory> into)
    {
        if (root.TryGetProperty(name, out JsonElement array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    into[item.GetString()!] = category;
            }
        }
    }
}
