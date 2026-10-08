using Mcc.Cli.Presentation;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// The bar shared by the health line and the advancement progress header.
/// A number says what a value is; a bar says where it sits between empty and full without the reader doing arithmetic.
/// </summary>
public sealed class MeterTests
{
    [Theory]
    [InlineData(0.0, "░░░░░░░░░░")]
    [InlineData(1.0, "▓▓▓▓▓▓▓▓▓▓")]
    [InlineData(0.5, "▓▓▓▓▓░░░░░")]
    [InlineData(0.25, "▓▓▓░░░░░░░")]
    public void FillsProportionally(double fraction, string expected)
        => Assert.Equal(expected, Meter.Render(fraction, 10, unicode: true));

    /// <summary>ASCII follows the glyph setting, like every other drawn thing.</summary>
    [Fact]
    public void AsciiModeUsesAsciiCells()
        => Assert.Equal("#####-----", Meter.Render(0.5, 10, unicode: false));

    /// <summary>
    /// A tiny non-zero value still fills one cell.
    /// Rounding 0.1% down to an empty bar says "no progress at all", which is a different claim from "barely any", and the second one is the true one.
    /// With 2 of 1584 advancements this is the normal case, not an edge case.
    /// </summary>
    [Fact]
    public void ASmallNonZeroValueStillShows()
    {
        string bar = Meter.Render(2.0 / 1584, 15, unicode: true);

        Assert.StartsWith("▓", bar, StringComparison.Ordinal);
        Assert.Equal(15, bar.Length);
    }

    /// <summary>And the converse: not quite full must not read as full.</summary>
    [Fact]
    public void NotQuiteFullIsNotDrawnAsFull()
    {
        string bar = Meter.Render(0.999, 10, unicode: true);

        Assert.Contains('░', bar);
    }

    /// <summary>A server can report more health than the vanilla maximum; the bar cannot overrun.</summary>
    [Theory]
    [InlineData(-5.0)]
    [InlineData(2.5)]
    [InlineData(double.NaN)]
    public void OutOfRangeValuesAreClamped(double fraction)
        => Assert.Equal(10, Meter.Render(fraction, 10, unicode: true).Length);

    /// <summary>Red below 40%, gold to 90%, green above: the bands the health bar colours by.</summary>
    [Theory]
    [InlineData(0.0, Meter.Band.Low)]
    [InlineData(0.39, Meter.Band.Low)]
    [InlineData(0.4, Meter.Band.Medium)]
    [InlineData(0.89, Meter.Band.Medium)]
    [InlineData(0.9, Meter.Band.High)]
    [InlineData(1.0, Meter.Band.High)]
    public void BandsSplitAtFortyAndNinety(double fraction, Meter.Band expected)
        => Assert.Equal(expected, Meter.Of(fraction));

    [Theory]
    [InlineData(Meter.Band.Low, 'c')]      // red
    [InlineData(Meter.Band.Medium, 'e')]   // gold
    [InlineData(Meter.Band.High, 'a')]     // green
    public void BandsMapToLegacyColours(Meter.Band band, char expected)
        => Assert.Equal(expected, Meter.Colour(band));

    /// <summary>
    /// A whole number reads better as "40%" than "40.0%", but rounding 0.13% to "0%" would state the opposite of what is true, so a small non-zero value keeps its decimal.
    /// </summary>
    [Theory]
    [InlineData(0.4, "40%")]
    [InlineData(1.0, "100%")]
    [InlineData(0.0, "0%")]
    [InlineData(2.0 / 1584, "0.1%")]
    public void PercentIsReadableAndInvariant(double fraction, string expected)
        => Assert.Equal(expected, Meter.Percent(fraction));

    /// <summary>The percentage never carries a comma decimal separator, whatever the machine locale is.</summary>
    [Fact]
    public void PercentIsInvariantOfCulture()
    {
        System.Globalization.CultureInfo original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal("0.1%", Meter.Percent(2.0 / 1584));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }
}
