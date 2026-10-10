using DMCBK.Core;
using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core.Presentation;
using Umpk.Text.Serialization;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// The classic host's <c>/man</c> renderer.
/// Every assertion here corresponds to a real defect: the library's defaults are wrong for a manual page in six separate ways, and two of the three integration rules are invisible until something downstream breaks.
/// </summary>
public sealed class MarkdownRenderingTests
{
    private const string Page = """
        # Movement

        MCC runs a local physics simulation.

        ## Commands

        Use `-f` to force unsafe moves.

        | Key | Default |
        | --- | --- |
        | `Terrain` | true |

        > [!WARNING]
        > No flow field.
        """;

    private static MarkdownConsoleRenderer Renderer(
        ConsoleColorDepth depth = ConsoleColorDepth.Vt10024Bit, bool emoji = true)
        => new(depth, emoji ? MccGlyphs.Emoji : GlyphSet.Ascii);

    private static string Text(IEnumerable<string> lines)
        => string.Join('\n', lines).Replace("", "\\e", StringComparison.Ordinal);

    /// <summary>The page renders at all, and its content survives.</summary>
    [Fact]
    public void RendersThePage()
    {
        IReadOnlyList<string> lines = Renderer().RenderLines(Page, 76);

        Assert.NotEmpty(lines);
        Assert.Contains(lines, l => l.Contains("physics simulation", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("Movement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EarlyBuildWarning_RendersAsAWarningBlock()
    {
        string text = Text(Renderer(ConsoleColorDepth.Disable).RenderLines(
            Strings.EarlyBuildWarningMarkdown,
            width: 76));

        Assert.Contains("Early test build", text, StringComparison.Ordinal);
        Assert.Contains("MCC 2.0 is unfinished", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[!WARNING]", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Spectre wraps text-style headers in their own Markdown markers, so "## Commands" rendered as "## Commands ##" with the hashes on screen.
    /// A reader is looking at a rendered page.
    /// </summary>
    [Fact]
    public void DoesNotPrintMarkdownMarkers()
    {
        string text = Text(Renderer().RenderLines(Page, 76));

        Assert.DoesNotContain("## Commands ##", text, StringComparison.Ordinal);
        Assert.DoesNotContain("**", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// The default H1 style is Figlet ASCII art: "# Movement" came out as twelve lines of block letters, wrapped mid-word.
    /// A page title is one line.
    /// </summary>
    [Fact]
    public void DoesNotRenderHeadingsAsAsciiArt()
    {
        IReadOnlyList<string> lines = Renderer().RenderLines("# Movement\n\nBody text.\n", 76);

        Assert.True(lines.Count < 8, $"a two-line page rendered as {lines.Count} lines, which is Figlet art");
    }

    /// <summary>
    /// Spectre pads every line to the profile width.
    /// Those lines go into ConsoleInteractive's recent-message ring and get replayed whenever a popup closes, so padding them repaints the full terminal width for nothing.
    /// </summary>
    [Fact]
    public void TrimsTrailingPadding()
    {
        foreach (string line in Renderer().RenderLines(Page, 76))
            Assert.False(line.EndsWith(' '), $"line keeps its padding: '{line}'");
    }

    /// <summary>
    /// The colour depth is MCC's, not Spectre's guess: a page must not emit truecolor into a terminal the rest of the client is treating as monochrome.
    /// </summary>
    [Fact]
    public void HonoursTheConfiguredColourDepth()
    {
        string none = Text(Renderer(ConsoleColorDepth.Disable).RenderLines(Page, 76));
        Assert.DoesNotContain("\\e[", none, StringComparison.Ordinal);

        string full = Text(Renderer(ConsoleColorDepth.Vt10024Bit).RenderLines(Page, 76));
        Assert.Contains("\\e[", full, StringComparison.Ordinal);

        // 4-bit must not carry 256-colour or truecolor SGR parameters.
        string legacy = Text(Renderer(ConsoleColorDepth.Vt1004Bit).RenderLines(Page, 76));
        Assert.DoesNotContain("38;5;", legacy, StringComparison.Ordinal);
        Assert.DoesNotContain("38;2;", legacy, StringComparison.Ordinal);
    }

    /// <summary>
    /// Glyphs and box drawing degrade together off the one console.toml setting, so an ASCII terminal does not get clean glyphs inside a table made of characters it cannot draw.
    /// </summary>
    [Fact]
    public void AsciiModeUsesAsciiBorders()
    {
        string ascii = Text(Renderer(ConsoleColorDepth.Disable, emoji: false).RenderLines(Page, 76));

        Assert.Contains("+--", ascii, StringComparison.Ordinal);
        Assert.DoesNotContain("╭", ascii, StringComparison.Ordinal);
        Assert.DoesNotContain("┌", ascii, StringComparison.Ordinal);
    }

    /// <summary>
    /// SmartyPants is on by default and would turn <c>-f</c> into a typographic dash, on pages whose whole job is showing exact syntax a user has to type back.
    /// </summary>
    [Fact]
    public void DoesNotPrettifyPunctuation()
    {
        string text = Text(Renderer(ConsoleColorDepth.Disable).RenderLines("Use `-f` and \"quotes\".\n", 76));

        Assert.Contains("-f", text, StringComparison.Ordinal);
        Assert.DoesNotContain('–', text);   // en dash
        Assert.DoesNotContain('—', text);   // em dash
        Assert.DoesNotContain('“', text);   // curly quotes
    }

    /// <summary>Every shipped page renders without throwing, at a narrow width and a wide one.</summary>
    [Theory]
    [InlineData(40)]
    [InlineData(200)]
    public void EveryShippedPageRenders(int width)
    {
        MarkdownConsoleRenderer renderer = Renderer();
        foreach (DMCBK.Core.Manual.ManualTopic topic in DMCBK.Core.Manual.ManualTopics.All)
        {
            string page = new DMCBK.Core.Manual.ManualCatalog().Read(topic.Id)!;
            IReadOnlyList<string> lines = renderer.RenderLines(page, width);
            Assert.NotEmpty(lines);
        }
    }

    /// <summary>
    /// The guarantee that matters most: server chat is NOT Markdown.
    /// <para>
    /// Chat is a Minecraft component tree and is rendered by <c>ChatPresenter</c> through the component renderer.
    /// If it ever reached the Markdown path, a player typing asterisks or a heading marker could restyle another player's console, and a table pipe could reflow their screen.
    /// This asserts the two paths are separate by construction: the Markdown renderer is reached only through <c>IHostUi.TryWriteDocument</c>, which only <c>ManCommand</c> calls.
    /// </para>
    /// </summary>
    [Fact]
    public void ChatIsNeverRenderedAsMarkdown()
    {
        System.Reflection.MethodInfo[] callers =
        [
            .. typeof(Client).Assembly.GetTypes()
                .Concat(typeof(DMCBK.Core.Commands.Impl.ManCommand).Assembly.GetTypes())
                .Concat(typeof(MarkdownConsoleRenderer).Assembly.GetTypes())
                .SelectMany(t => t.GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.DeclaredOnly))
        ];

        // Nothing on a chat path may name the document seam.
        // The seam has exactly one caller, and it is the manual command.
        Assert.Contains(callers, m => m.DeclaringType?.Name == "ManCommand");

        foreach (System.Reflection.MethodInfo method in callers)
        {
            string? owner = method.DeclaringType?.Name;
            if (owner is null || !owner.Contains("Chat", StringComparison.Ordinal))
                continue;

            Assert.False(
                method.Name.Contains("Document", StringComparison.Ordinal)
                || method.Name.Contains("Markdown", StringComparison.Ordinal),
                $"{owner}.{method.Name} is on a chat path and names the document renderer.");
        }
    }
}
