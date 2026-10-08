using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core.Localization;
using Umpk.Text.Serialization;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// MCC's own <c>§§X</c> BACKGROUND extension, rendered for the classic host.
/// <para>
/// Reported: the chunk map draws no player or marked-chunk marker.
/// Those two cells are the only users of the extension (<c>§§7</c> grey for the player's chunk, <c>§§4</c> red for the marked one, and the legend that explains them), and the classic host's render path never understood it.
/// UMPK's <c>LegacyText</c> owns <c>§X</c>, vanilla's foreground code, and has never heard of <c>§§X</c>; its rule for an unrecognised code is to keep the first character and re-read from the second, so <c>§§7</c> parsed as a literal <c>§</c> plus the REAL code <c>§7</c>.
/// The lone section sign was then dropped by the flattener's end-of-string rule and the cell came out with a dark grey FOREGROUND.
/// On the legend line, whose cells are two spaces, a foreground colour is invisible ink.
/// </para>
/// </summary>
public sealed class LegacyBackgroundTests
{
    private const string Esc = "\u001b";

    private static AnsiComponentRenderer Renderer(ConsoleColorDepth depth = ConsoleColorDepth.Vt10024Bit)
        => new(new HostTranslations(), depth);

    private static string Render(string line, ConsoleColorDepth depth = ConsoleColorDepth.Vt10024Bit)
        => LegacyBackground.Render(line, Renderer(depth), depth);

    [Fact]
    public void ThePremise_LegacyTextAloneLosesTheBackgroundCode()
    {
        // Why this class exists at all, pinned so it is not taken on trust: hand §§7 to the parser that used to do this job and the marker does not survive as a background.
        string throughUmpkAlone = Renderer().Render(LegacyText.Parse("§§7  §§r"));

        Assert.DoesNotContain($"{Esc}[48;", throughUmpkAlone, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLegendCells_RenderAsABackgroundAroundTheirSpaces()
    {
        // The exact legend fragment: two spaces on grey, then off again.
        string rendered = Render("§§7  §§r");

        Assert.StartsWith($"{Esc}[48;2;", rendered, StringComparison.Ordinal);
        Assert.Contains("  ", rendered, StringComparison.Ordinal);
        Assert.EndsWith($"{Esc}[0m", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMarkedChunkCell_UsesADifferentBackgroundFromThePlayerCell()
    {
        string marked = Render("§§4  §§r");

        Assert.StartsWith($"{Esc}[48;2;", marked, StringComparison.Ordinal);
        Assert.NotEqual(Render("§§7  §§r"), marked);
    }

    [Fact]
    public void AMapCell_KeepsItsGlyphInsideTheBackground()
    {
        // A map row draws §§7 + the status glyph + §§r.
        // The glyph must come through untouched.
        string rendered = Render("§§7\U0001F7E8§§r");

        Assert.Contains("\U0001F7E8", rendered, StringComparison.Ordinal);
        Assert.StartsWith($"{Esc}[48;2;", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void ALineWithNoBackgroundMarker_IsRenderedExactlyAsBefore()
    {
        // The overwhelming majority of output.
        // It must go through the ordinary path byte for byte.
        foreach (string line in new[] { "plain text", "§ccoloured", "§9§nblue underlined§r", "x§8y" })
            Assert.Equal(Renderer().Render(LegacyText.Parse(line)), Render(line));
    }

    [Fact]
    public void ForegroundAndBackgroundCoexist()
    {
        // §§7 sets the background, §c the foreground; both must reach the terminal.
        string rendered = Render("§§7§cred on grey§§r");

        Assert.Contains($"{Esc}[48;2;", rendered, StringComparison.Ordinal);
        Assert.Contains($"{Esc}[38;2;255;85;85m", rendered, StringComparison.Ordinal);
        Assert.Contains("red on grey", rendered, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(24)]
    public void EveryColourDepth_ProducesABackgroundTheTerminalUnderstands(int bits)
    {
        ConsoleColorDepth depth = bits switch
        {
            24 => ConsoleColorDepth.Vt10024Bit,
            8 => ConsoleColorDepth.Vt1008Bit,
            _ => ConsoleColorDepth.Vt1004Bit,
        };

        string rendered = Render("§§4  §§r", depth);

        // 4-bit backgrounds are 40-47/100-107; the 8-bit and 24-bit forms use the 48 selector.
        bool hasBackground = rendered.Contains($"{Esc}[48;", StringComparison.Ordinal)
            || rendered.Contains($"{Esc}[41m", StringComparison.Ordinal)
            || rendered.Contains($"{Esc}[101m", StringComparison.Ordinal);

        Assert.True(hasBackground, $"no background sequence at {depth}");
    }

    [Fact]
    public void ColourDisabled_DrawsNoBackgroundAndKeepsTheText()
    {
        // Not a fallback background: no colour means no colour, and the cell content still comes through.
        Assert.Equal("ab", Render("§§7ab§§r", ConsoleColorDepth.Disable));
    }

    [Fact]
    public void TheBackgroundSequences_SurviveTheSuggestionPopup()
    {
        // The popup rebuilds covered lines from a ring and discards any escape its colour-only regexes do not classify (see SuggestionPopupRestoreTests).
        // A chunk map is exactly the sort of wide output a popup lands on, so its background codes have to be on that list: 48;2;R;G;B is, and ESC[49m, the narrower "default background", is NOT, which is why the reset here is a full reset.
        string rendered = Render("§§7  §§4  §§r");

        Assert.DoesNotContain($"{Esc}[49m", rendered, StringComparison.Ordinal);
        Assert.Contains($"{Esc}[0m", rendered, StringComparison.Ordinal);
    }

    [Fact]
    public void SomethingThatIsNotABackgroundCode_IsLeftAlone()
    {
        // §§ followed by a character with no colour behind it is not the extension; it must not be eaten.
        Assert.DoesNotContain($"{Esc}[48;", Render("§§z"), StringComparison.Ordinal);
    }

    [Fact]
    public void ATrailingMarkerDoesNotRunOffTheEnd()
    {
        // A marker is three characters.
        // Two at the very end are not one, and must not throw or truncate.
        Assert.Contains("ab", Render("ab§§"), StringComparison.Ordinal);
        Assert.Contains("ab", Render("ab§"), StringComparison.Ordinal);
    }

    [Fact]
    public void HasBackground_IsTheCheapPrecheck()
    {
        Assert.True(LegacyBackground.HasBackground("§§7  §§r"));
        Assert.False(LegacyBackground.HasBackground("§7 plain"));
        Assert.False(LegacyBackground.HasBackground(null));
    }
}
