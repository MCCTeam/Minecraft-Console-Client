using Mcc.Cli.Presentation;
using System.Text;
using System.Text.RegularExpressions;
using Mcc.Cli;
using DMCBK.Core.Localization;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// Everything the classic host writes must survive the suggestion popup passing over it.
/// <para>
/// The popup does not read the screen to find out what it is covering; it reconstructs those lines from a 32-slot ring of recent writes.
/// These tests model the corrected style and character-width behavior so a regression on either side shows up here:
/// </para>
/// <list type="number">
///   <item><description>
/// Style.
/// Every escape used to be classified with six anchored colour-only regexes and anything else discarded, so a decoration (<c>ESC[1m</c>) never reached the restore and a colour combined with a decoration in one sequence (<c>ESC[38;2;255;255;85;1m</c>) was thrown away whole, taking the colour with it.
/// On a server whose MOTD is a bold gradient, every character carried such a pair, so the whole line came back plain.
/// Every SGR escape is kept now, and the restore replays the range from the last reset rather than seeding one code per colour list.
///   </description></item>
///   <item><description>
/// Width.
/// The walk that finds the covered slice advanced one UTF-16 unit at a time, so it could stop between the halves of a surrogate pair and hand the terminal a lone low surrogate.
/// The chunk map is made of astral emoji, which is where it showed.
/// The walk advances by whole characters now.
///   </description></item>
/// </list>
/// <para>
/// The renderer still emits the colour and each decoration as separate sequences.
/// That split is no longer needed by the fixed restore, but it costs nothing and keeps the output readable by a consumer that does not contain the suggestion-popup fix, so it stays.
/// </para>
/// </summary>
public sealed class SuggestionPopupRestoreTests
{
    private const string Esc = "\u001b";

    /// <summary>The one regex the fixed restore still uses (ConsoleSuggestion.RecentMessage).</summary>
    private static readonly Regex EscapeCodeRegex = new(@"\u001B\[[\d;]+m", RegexOptions.Compiled);

    private const string ResetColorCode = "\u001b[0m";

    private static AnsiComponentRenderer Renderer(ConsoleColorDepth depth)
        => new(new HostTranslations(), depth);

    // The theories are parameterised by bit depth rather than by the enum itself: ConsoleColorDepth is internal to Mcc.Cli, and a public xunit theory cannot take an internal parameter type.
    private static ConsoleColorDepth Depth(int bits) => bits switch
    {
        24 => ConsoleColorDepth.Vt10024Bit,
        8 => ConsoleColorDepth.Vt1008Bit,
        _ => ConsoleColorDepth.Vt1004Bit,
    };

    /// <summary>Every colour vanilla's <c>ChatFormatting</c> declares, by the name a component carries.</summary>
    private static readonly string[] NamedColors =
    [
        "black", "dark_blue", "dark_green", "dark_aqua", "dark_red", "dark_purple", "gold", "gray",
        "dark_gray", "blue", "green", "aqua", "red", "light_purple", "yellow", "white",
    ];

    private static Style StyleWith(TextColor? color, int decorations) => new()
    {
        Color = color,
        Bold = (decorations & 1) != 0 ? true : null,
        Italic = (decorations & 2) != 0 ? true : null,
        Underlined = (decorations & 4) != 0 ? true : null,
        Strikethrough = (decorations & 8) != 0 ? true : null,
        Obfuscated = (decorations & 16) != 0 ? true : null,
    };

    #region The submodule's restore, modelled

    /// <summary>
    /// <c>ConsoleSuggestion.RecentMessage.ParseColorCode</c>: the line split into plain text plus every SGR escape, each tagged with the index in that plain text where it takes effect.
    /// </summary>
    private static (string Plain, List<(int Index, string Code)> Codes) Parse(string rendered)
    {
        var codes = new List<(int, string)>();
        int consumed = 0;
        foreach (Match match in EscapeCodeRegex.Matches(rendered))
        {
            codes.Add((match.Index - consumed, match.Value));
            consumed += match.Length;
        }

        return (EscapeCodeRegex.Replace(rendered, string.Empty), codes);
    }

    /// <summary>
    /// <c>GetBgMessageBuffer</c>'s style seeding and replay: everything from the last reset at or before the cut, then the codes inside the window, interleaved with the text.
    /// </summary>
    private static string RestoreWindow(string rendered, int start, int length)
    {
        (string plain, List<(int Index, string Code)> codes) = Parse(rendered);
        int charStart = Math.Min(start, plain.Length);
        int charEnd = Math.Min(start + length, plain.Length);

        int from = 0;
        for (int i = codes.Count - 1; i >= 0; i--)
        {
            if (codes[i].Index <= charStart && codes[i].Code == ResetColorCode)
            {
                from = i;
                break;
            }
        }

        var sb = new StringBuilder();
        int idx = from;
        while (idx < codes.Count && codes[idx].Index < charStart)
            sb.Append(codes[idx++].Code);

        for (int i = charStart; i < charEnd; i++)
        {
            while (idx < codes.Count && codes[idx].Index == i)
                sb.Append(codes[idx++].Code);

            sb.Append(plain[i]);
        }

        return sb.ToString();
    }

    /// <summary>The SGR parameters in effect at the end of an escape stream, as the terminal would hold them.</summary>
    private static HashSet<string> ActiveParameters(string escaped)
    {
        var active = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in EscapeCodeRegex.Matches(escaped))
        {
            if (match.Value == ResetColorCode)
            {
                active.Clear();
                continue;
            }

            active.Add(match.Value[2..^1]);
        }

        return active;
    }

    #endregion
    #region Style survives the popup

    [Theory]
    [InlineData(24)]
    [InlineData(8)]
    [InlineData(4)]
    public void EveryColourAndDecorationCombination_SurvivesACoveredWindow(int bits)
    {
        // 16 named colours (plus no colour) against all 32 subsets of the five decorations: every style a vanilla component can carry, covered from every position, each checked for the style the original had there.
        AnsiComponentRenderer renderer = Renderer(Depth(bits));
        var failures = new List<string>();

        foreach (string? name in NamedColors.Append(null))
        {
            TextColor? color = name is null ? null : TextColor.FromName(name);
            for (int decorations = 0; decorations < 32; decorations++)
            {
                string rendered = renderer.Render(
                    new Component(new TextContent("sample"), StyleWith(color, decorations)));

                // The style the line carries over its text, i.e. everything before the trailing reset.
                int lastReset = rendered.LastIndexOf(ResetColorCode, StringComparison.Ordinal);
                HashSet<string> whole = ActiveParameters(lastReset < 0 ? rendered : rendered[..lastReset]);

                for (int start = 0; start < 4; start++)
                {
                    HashSet<string> restored = ActiveParameters(RestoreWindow(rendered, start, 2));
                    if (!whole.SetEquals(restored))
                    {
                        failures.Add(
                            $"{name ?? "(no colour)"} decorations={decorations} at {start}: "
                            + $"expected [{string.Join(',', whole)}] got [{string.Join(',', restored)}]");
                    }
                }
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void TheBoldGradient_TheReportedCase_KeepsBothColourAndBold()
    {
        // The exact shape that broke: one component per character, each with its own interpolated colour AND bold.
        // Previously this restored with neither.
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Vt10024Bit);
        const string text = "Dobrodosao milutinke na Jedini Pravi Balkanski Server!";

        var children = new List<Component>(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            int r = 255 - (255 * i / (text.Length - 1));
            int b = 255 * i / (text.Length - 1);
            children.Add(new Component(
                new TextContent(text[i].ToString()),
                new Style { Color = TextColor.FromRgb((r << 16) | b), Bold = true }));
        }

        string rendered = renderer.Render(new Component(new TextContent(string.Empty), new Style(), children));

        for (int start = 0; start < text.Length - 1; start++)
        {
            HashSet<string> active = ActiveParameters(RestoreWindow(rendered, start, 20));

            Assert.Contains("1", active); // bold
            Assert.Contains(active, p => p.StartsWith("38;2;", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void AWindowOpeningMidRun_IsSeededWithThatRunsExactStyle()
    {
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Vt10024Bit);
        var line = new Component(
            new TextContent(string.Empty),
            new Style(),
            [
                new Component(new TextContent("aaaaa"), new Style { Color = TextColor.Yellow, Bold = true }),
                new Component(new TextContent("bbbbb"), new Style { Color = TextColor.Red, Underlined = true }),
            ]);

        string rendered = renderer.Render(line);

        HashSet<string> inYellow = ActiveParameters(RestoreWindow(rendered, 2, 2));
        Assert.Contains("38;2;255;255;85", inYellow);
        Assert.Contains("1", inYellow);

        HashSet<string> inRed = ActiveParameters(RestoreWindow(rendered, 7, 2));
        Assert.Contains("38;2;255;85;85", inRed);
        Assert.Contains("4", inRed);
        Assert.DoesNotContain("1", inRed); // the reset between the runs really does clear the bold
    }

    [Fact]
    public void LegacySectionCodes_IncludingTheHexExtension_RestoreTheirTextExactly()
    {
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Vt10024Bit);
        string[] lines =
        [
            "§0a§1b§2c§3d§4e§5f§6g§7h§8i§9j§ak§bl§cm§dn§eo§fp",
            "§k obf §l bold §m strike §n under §o italic §r reset",
            "§c§lred bold§r §9§nblue underlined§r",
            "§#ff0000R§#00ff00G§#0000ffB",
            "§#ff8800§lgradient start§#0088ffend",
        ];

        foreach (string line in lines)
        {
            string rendered = renderer.Render(Umpk.Text.Serialization.LegacyText.Parse(line));
            (string plain, _) = Parse(rendered);

            for (int start = 0; start < plain.Length; start++)
            {
                (string restoredPlain, _) = Parse(RestoreWindow(rendered, start, 4));
                Assert.Equal(plain[start..Math.Min(start + 4, plain.Length)], restoredPlain);
            }
        }
    }

    #endregion
    #region Width: astral characters are never split

    /// <summary>
    /// <c>GetBgMessageBuffer</c>'s walk, in its fixed form: advance by whole characters, so a surrogate pair is one step of its full width rather than two steps of one.
    /// </summary>
    private static int CharIndexAtColumn(string text, int column)
    {
        int cursor = 0, index = 0;
        while (cursor < column && index < text.Length)
        {
            bool pair = char.IsHighSurrogate(text[index])
                && index + 1 < text.Length
                && char.IsLowSurrogate(text[index + 1]);
            int width = pair
                ? Math.Max(0, Wcwidth.UnicodeCalculator.GetWidth(new Rune(text[index], text[index + 1])))
                : Math.Max(0, Wcwidth.UnicodeCalculator.GetWidth(text[index]));

            if (cursor + width > column)
                break;

            cursor += width;
            index += pair ? 2 : 1;
        }

        return index;
    }

    [Fact]
    public void APopupEdge_NeverLandsInsideACharacter()
    {
        // A chunk-map row: eight astral emoji, each two columns wide.
        // Every column the popup could open at must map to a whole character.
        // Before the fix, every odd column landed on a low surrogate and the terminal was handed half a character.
        string row = string.Concat(Enumerable.Repeat("\U0001F7E8", 8));

        for (int column = 0; column <= 16; column++)
        {
            int at = CharIndexAtColumn(row, column);

            Assert.False(
                at < row.Length && char.IsLowSurrogate(row[at]),
                $"column {column} cut a surrogate pair at char index {at}");
        }
    }

    [Theory]
    [InlineData("\U0001F7E8")] // large yellow square, the chunk map's "loading"
    [InlineData("\U0001F7E9")] // large green square, "loaded"
    [InlineData("\U0001F533")] // white square button, "not received"
    [InlineData("\U0001F6AB")] // no entry sign, the plugin listing's "disabled"
    [InlineData("⬜")]          // a WIDE character inside the BMP: one unit, still two columns
    [InlineData("✅")]          // the plugin listing's "enabled"
    public void EveryGlyphThisClientDraws_IsWalkedAsOneCharacter(string glyph)
    {
        string row = string.Concat(Enumerable.Repeat(glyph, 5));

        for (int column = 0; column <= 10; column++)
        {
            int at = CharIndexAtColumn(row, column);
            Assert.False(at < row.Length && char.IsLowSurrogate(row[at]), $"column {column} split {glyph}");
        }
    }

    [Fact]
    public void ARestoredSliceOfEmoji_IsValidText()
    {
        // The user-visible consequence: whatever the popup hands back has to be well-formed UTF-16, or the terminal draws replacement glyphs where the emoji were.
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Vt10024Bit);
        string rendered = renderer.Render(Component.Text(string.Concat(Enumerable.Repeat("\U0001F7E8", 8))));
        (string plain, _) = Parse(rendered);

        for (int start = 0; start <= 16; start++)
        {
            int at = CharIndexAtColumn(plain, start);
            string slice = plain[at..];

            Assert.False(
                slice.Length > 0 && char.IsLowSurrogate(slice[0]),
                $"slice at column {start} starts mid-character");
        }
    }

    #endregion
    #region The renderer's own shape

    [Fact]
    public void ColourAndDecoration_AreStillSeparateSequences()
    {
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Vt10024Bit);
        var style = new Style { Color = TextColor.FromRgb(0x1E90FF), Bold = true, Underlined = true };

        Assert.Equal(
            $"{Esc}[38;2;30;144;255m{Esc}[1m{Esc}[4mx{Esc}[0m",
            renderer.Render(new Component(new TextContent("x"), style)));
    }

    [Fact]
    public void AnUndecoratedRun_IsUnchanged()
    {
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Vt10024Bit);

        Assert.Equal(
            $"{Esc}[38;2;255;85;85mhi{Esc}[0m",
            renderer.Render(new Component(new TextContent("hi"), new Style { Color = TextColor.Red })));
    }

    [Fact]
    public void AnUnstyledRun_EmitsNoEscapesAtAll()
        => Assert.Equal("plain", Renderer(ConsoleColorDepth.Vt10024Bit).Render(Component.Text("plain")));

    [Fact]
    public void ColorDisabled_EmitsNoEscapesForAnyCombination()
    {
        AnsiComponentRenderer renderer = Renderer(ConsoleColorDepth.Disable);

        for (int decorations = 0; decorations < 32; decorations++)
        {
            Assert.Equal(
                "x",
                renderer.Render(new Component(new TextContent("x"), StyleWith(TextColor.Red, decorations))));
        }
    }

    [Fact]
    public void TheCombinedForm_IsNoLongerAProblemEither()
    {
        // The submodule keeps every SGR escape now, so the compact form a different writer might emit restores as well as the split one.
        // Pinned because it is the fix's whole point: the restore no longer has an opinion about which escapes are worth keeping.
        string combined = $"{Esc}[38;2;255;255;85;1mtext{Esc}[0m";

        Assert.Contains("38;2;255;255;85;1", ActiveParameters(RestoreWindow(combined, 1, 2)));
    }
    #endregion
}
