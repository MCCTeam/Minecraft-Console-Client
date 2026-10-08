namespace Mcc.Cli.Tui.Minimap;

/// <summary>A preset anchor for positioning the movable minimap window.</summary>
internal enum MinimapPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center,
}

/// <summary>The entity category used for minimap coloring (ported from the legacy classifier).</summary>
internal enum MobCategory
{
    Hostile,
    Passive,
    Neutral,
    Player,
    NonLiving,
}

/// <summary>
/// Converts a namespaced registry id (block or entity, e.g. <c>minecraft:acacia_door</c>) to the PascalCase enum-style key the ported minimap JSON is keyed by (<c>AcaciaDoor</c>).
/// The legacy JSON was generated from MCC's <c>Material</c>/<c>EntityType</c> enum names; the new core exposes namespaced ids, so this bridges the two without regenerating the data.
/// </summary>
internal static class RegistryKey
{
    public static string ToPascal(string namespacedId)
    {
        if (string.IsNullOrEmpty(namespacedId))
            return string.Empty;

        int colon = namespacedId.IndexOf(':');
        ReadOnlySpan<char> path = colon >= 0 ? namespacedId.AsSpan(colon + 1) : namespacedId.AsSpan();

        Span<char> buffer = path.Length <= 128 ? stackalloc char[path.Length] : new char[path.Length];
        int len = 0;
        bool upNext = true;
        foreach (char c in path)
        {
            if (c == '_')
            {
                upNext = true;
                continue;
            }

            buffer[len++] = upNext ? char.ToUpperInvariant(c) : c;
            upNext = false;
        }

        return new string(buffer[..len]);
    }
}
