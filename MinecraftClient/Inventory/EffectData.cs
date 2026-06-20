namespace MinecraftClient.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Message;

/// <summary>
/// Represents an active status effect on an entity
/// </summary>
public class EffectData
{
    /// <summary>
    /// The type of effect
    /// </summary>
    public Effects Effect { get; set; }

    /// <summary>
    /// Effect amplifier (level - 1, e.g., 0 = level I, 1 = level II)
    /// </summary>
    public int Amplifier { get; set; }

    /// <summary>
    /// Duration in ticks (20 ticks = 1 second). -1 for infinite.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Effect flags (ambient, show particles, show icon)
    /// </summary>
    public byte Flags { get; set; }

    /// <summary>
    /// Time when the effect was applied
    /// </summary>
    public DateTime StartTime { get; set; }

    public EffectData(Effects effect, int amplifier, int duration, byte flags)
    {
        Effect = effect;
        Amplifier = amplifier;
        Duration = duration;
        Flags = flags;
        StartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this is an infinite duration effect
    /// </summary>
    public bool IsInfinite => Duration == -1 || Duration == int.MaxValue;

    /// <summary>
    /// Check if the effect has expired
    /// </summary>
    public bool IsExpired
    {
        get
        {
            if (IsInfinite) return false;
            return GetElapsedTicks() >= Duration;
        }
    }

    /// <summary>
    /// Get remaining duration in ticks
    /// </summary>
    public int RemainingTicks
    {
        get
        {
            if (IsInfinite) return -1;
            return Math.Max(0, Duration - GetElapsedTicks());
        }
    }

    /// <summary>
    /// Get remaining duration in seconds
    /// </summary>
    public int RemainingSeconds
    {
        get
        {
            if (IsInfinite) return -1;
            return (RemainingTicks + 19) / 20;
        }
    }

    /// <summary>
    /// Get the translated effect name from Minecraft translations
    /// </summary>
    public string GetTranslatedName()
    {
        var key = $"effect.minecraft.{Effect.ToString().ToUnderscoreCase()}";
        var translated = ChatParser.TranslateString(key);
        return string.IsNullOrEmpty(translated) ? Effect.ToString() : translated;
    }

    /// <summary>
    /// Get the translated effect name with level when applicable
    /// </summary>
    public string GetDisplayName()
    {
        string translatedName = GetTranslatedName();
        if (Amplifier <= 0)
            return translatedName;

        return string.Format(Translations.effect_name_with_amplifier, translatedName,
            EnchantmentMapping.ConvertLevelToRomanNumbers(Amplifier + 1));
    }

    /// <summary>
    /// Get the translated effect name prefixed with the best-fit indefinite article
    /// </summary>
    public string GetDisplayNameWithArticle()
    {
        string displayName = GetDisplayName();
        char? firstLetter = displayName
            .TrimStart()
            .FirstOrDefault(char.IsLetter);

        if (firstLetter is null)
            return displayName;

        string article = "AEIOUaeiou".Contains(firstLetter.Value)
            ? Translations.effect_article_an
            : Translations.effect_article_a;
        return $"{article} {displayName}";
    }

    /// <summary>
    /// Get the configured short duration label for the remaining time
    /// </summary>
    public string GetRemainingDurationText()
    {
        return FormatShortDuration(RemainingSeconds);
    }

    /// <summary>
    /// Get the configured short duration label for the initial effect duration
    /// </summary>
    public string GetInitialDurationText()
    {
        if (IsInfinite)
            return Translations.effect_duration_unlimited;

        int durationSeconds = (Duration + 19) / 20;
        return FormatShortDuration(durationSeconds);
    }

    /// <summary>
    /// Format a duration for compact UI output
    /// </summary>
    /// <param name="seconds">Duration in seconds, -1 for unlimited</param>
    public static string FormatShortDuration(int seconds)
    {
        if (seconds < 0)
            return Translations.effect_duration_short_unlimited;

        if (seconds < 60)
            return string.Format(Translations.effect_duration_short_seconds, seconds);

        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;
        if (seconds < 3600)
        {
            return remainingSeconds == 0
                ? string.Format(Translations.effect_duration_short_minutes, minutes)
                : string.Format(Translations.effect_duration_short_minutes_seconds, minutes, remainingSeconds);
        }

        int hours = seconds / 3600;
        int remainingMinutes = (seconds % 3600) / 60;
        return remainingMinutes == 0
            ? string.Format(Translations.effect_duration_short_hours, hours)
            : string.Format(Translations.effect_duration_short_hours_minutes, hours, remainingMinutes);
    }

    private int GetElapsedTicks()
    {
        return (int)((DateTime.UtcNow - StartTime).TotalMilliseconds / 50);
    }
}

/// <summary>
/// Extension method for converting PascalCase to snake_case
/// </summary>
public static class StringExtensions
{
    public static string ToUnderscoreCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
    }
}
