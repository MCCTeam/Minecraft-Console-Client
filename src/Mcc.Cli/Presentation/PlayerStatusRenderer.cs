using System.Globalization;
using System.Text;
using DMCBK.Core.Localization;
using DMCBK.Core.Presentation;
using DMCBK.Core;

namespace Mcc.Cli.Presentation;

internal static class PlayerStatusRenderer
{
    public static string Render(PlayerStatus status, GlyphSet glyphs)
    {

        // Vanilla's maxima.
        // Health can exceed 20 with attributes or absorption, so the fraction is clamped by Meter rather than trusted; food never does.
        const float MaxHealth = 20f;
        const int MaxFood = 20;

        double fraction = MaxHealth <= 0 ? 0 : status.Health / MaxHealth;
        char colour = Meter.Colour(Meter.Of(fraction));

        // The maxima, the saturation and the xp total are SECONDARY, not decorative: "20/20" only means something if the "/20" can be read. §7 (170,170,170) is the secondary level; §8 (85,85,85) is a dark grey that vanishes against a dark terminal.
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"§{colour}{glyphs.Health}§r {FormatHealth(status.Health)}§7/{MaxHealth:0}§r");
        sb.Append(CultureInfo.InvariantCulture, $"   §6{glyphs.Food}§r {status.Food}§7/{MaxFood}§r");
        sb.Append(CultureInfo.InvariantCulture, $" §7({McStrings.Get("cmd.health.saturation")} {status.Saturation:0.#})§r");
        sb.Append(CultureInfo.InvariantCulture, $"   §b{glyphs.Experience}§r {McStrings.Get("cmd.health.level")} {status.ExperienceLevel}");
        sb.Append(CultureInfo.InvariantCulture, $" §7({status.TotalExperience} {McStrings.Get("cmd.health.xp")})§r");

        // The bar says where the number sits between empty and full without the reader doing arithmetic, and its colour says whether that position is a problem.
        // Red below 40%, gold to 90%, green above.
        sb.Append('\n')
          .Append('§').Append(colour)
          .Append(Meter.Render(fraction, BarWidth, glyphs.IsEmoji))
          .Append("§r §7").Append(Label(fraction)).Append("§r");

        return sb.ToString();
    }

    private static string FormatHealth(float health) => health.ToString("0.##", CultureInfo.CurrentCulture);

    /// <summary>The bar width, in cells. One per half-heart, so it reads like the vanilla health row.</summary>
    private const int BarWidth = 20;

    /// <summary>A word for the level the bar is at, so the colour is not the only thing carrying it.</summary>
    private static string Label(double fraction) => Meter.Of(fraction) switch
    {
        Meter.Band.High => McStrings.Get("cmd.health.state_full"),
        Meter.Band.Medium => McStrings.Get("cmd.health.state_hurt"),
        _ => McStrings.Get("cmd.health.state_critical"),
    };
}
