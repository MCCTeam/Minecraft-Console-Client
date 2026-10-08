using System.Text.RegularExpressions;
using BoxOfYellow.ConsoleMarkdownRenderer.Spectre;
using BoxOfYellow.ConsoleMarkdownRenderer.Spectre.Styling;
using DMCBK.Core.Presentation;
using Spectre.Console;
using Spectre.Console.Rendering;
using Umpk.Text.Serialization;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Renders a <c>/man</c> page for the classic console: Markdown in, terminal lines out.
/// <para>
/// Three rules govern this renderer.
/// </para>
/// <list type="number">
///   <item><description>
/// <b>The library never touches the console.</b> Only the <c>.Spectre</c> package is referenced, whose <c>MarkdownRenderer.Render</c> is a pure markdown-to-object-tree transform.
/// The top-level <c>BoxOfYellow.ConsoleMarkdownRenderer</c> package writes to <c>AnsiConsole</c> itself and prompts for interactive link selection, which would bypass <c>ConsoleWriter</c> and desynchronise the suggestion popup's recent-message ring.
/// That failure mode is documented at the top of <see cref="HostConsole"/>.
///   </description></item>
///   <item><description>
/// <b>Output goes out through <see cref="HostConsole.WriteLine"/>, not <c>WriteFormatted</c>.</b> Spectre emits raw SGR escapes; <c>WriteFormatted</c> runs its input through <c>LegacyBackground</c> and the section-sign renderer, which expect <c>§</c> codes.
/// Two ANSI producers on one line corrupt it.
/// This is the one output path in the classic host that must skip the <c>§</c> renderer.
///   </description></item>
///   <item><description>
/// <b>Trailing padding is trimmed.</b> Spectre pads every line to the full profile width; measured at 64 columns that was 1021 wasted characters on a single 41-line page.
/// Those lines go into ConsoleInteractive's 32-slot recent-message ring and get replayed whenever a popup closes, so padding them means repainting the full terminal width for nothing.
///   </description></item>
/// </list>
/// <para>
/// Chat from the server never comes here.
/// Chat is a Minecraft component tree rendered by <c>ChatPresenter</c>; treating it as Markdown would let a player's message restyle the console by typing asterisks.
/// </para>
/// </summary>
internal sealed partial class MarkdownConsoleRenderer
{
    private readonly MarkdownRenderer _renderer = new();
    private readonly SpectreDisplayOptions _options;
    private readonly ColorSystem _colors;

    public MarkdownConsoleRenderer(ConsoleColorDepth depth, GlyphSet glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        _colors = ToColorSystem(depth);
        _options = Theme(glyphs.IsEmoji);
    }

    /// <summary>Renders one page and writes it, line by line, through the console sink.</summary>
    public void Write(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        foreach (string line in RenderLines(markdown, Width()))
            HostConsole.WriteLine(line);
    }

    /// <summary>Renders one page to lines. Separated from <see cref="Write"/> so tests can assert on it.</summary>
    internal IReadOnlyList<string> RenderLines(string markdown, int width)
    {
        MarkdownRenderResult result = _renderer.Render(markdown, _options);
        if (result.Root is not IRenderable root)
            return [];

        var writer = new StringWriter();
        IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = _colors == ColorSystem.NoColors ? AnsiSupport.No : AnsiSupport.Yes,
            ColorSystem = (ColorSystemSupport)_colors,
            Out = new AnsiConsoleOutput(writer),
        });

        // Spectre's GitHub Actions enricher can re-enable ANSI after creation.
        // MCC's explicit output setting takes precedence over environment detection.
        console.Profile.Capabilities.Ansi = _colors != ColorSystem.NoColors;
        console.Profile.Width = width;
        console.Write(root);

        return [.. writer.ToString().TrimEnd('\n').Split('\n').Select(Tidy)];
    }

    /// <summary>
    /// Strips the padding Spectre adds to fill the profile width.
    /// Plain trailing spaces go, and so do spaces sitting between content and a trailing SGR reset, which a naive TrimEnd cannot see.
    /// At 64 columns this was 1021 characters on a single 41-line page, and every one of them would be stored in ConsoleInteractive's recent-message ring and repainted whenever a popup closed.
    /// </summary>
    private static string Tidy(string line)
    {
        line = TrailingPadding().Replace(line, "$1");
        return line.TrimEnd(' ');
    }

    [GeneratedRegex(@" +((?:\e\[[0-9;]*m)+)$")]
    private static partial Regex TrailingPadding();

    private static int Width()
    {
        try
        {
            // Two columns of headroom: a line drawn to the exact terminal width wraps on some terminals that count the final cell as an overflow.
            int width = Console.WindowWidth - 2;
            return width is >= 40 and <= 200 ? width : 78;
        }
        catch (IOException)
        {
            return 78;
        }
    }

    /// <summary>
    /// A theme that looks like MCC rather than like Spectre's defaults, every entry of which overrides something measurably wrong for a manual page.
    /// </summary>
    private static SpectreDisplayOptions Theme(bool emoji) => new()
    {
        // Spectre's default H1 is FIGLET ASCII ART: "# MOVEMENT(7)" came out as twelve lines of block letters, wrapped mid-word.
        // A rule reads as a page header and costs one line.
        Headers =
        [
            new SpectreRuleHeaderStyle(justification: Justify.Left, foreground: Color.Grey50),
            new SpectreTextStyle(Decoration.Bold, Color.Yellow),
            new SpectreTextStyle(Decoration.Bold, Color.Aqua),
        ],

        // The default carries Decoration.Invert, i.e. reverse video, for every remaining level.
        Header = new SpectreTextStyle(Decoration.Bold),

        // Text-style headers are wrapped in their own markdown markers by default, so "## Commands" rendered as "## Commands ##" with the hashes on screen.
        // A reader is looking at a rendered page; the source syntax is not part of it.
        WrapHeader = false,

        // Default is yellow ON BLUE, which fights whatever background the terminal already has.
        CodeInLine = new Style(foreground: Color.Aqua),
        CodeBlock = new Style(foreground: Color.Grey85),

        // Emoji and box drawing degrade together, off the one console.toml Glyphs setting, so an ASCII terminal does not get clean glyphs inside a table made of characters it cannot draw.
        TableBorder = emoji ? TableBorder.Rounded : TableBorder.Ascii,
        AlertPanelBorder = emoji ? BoxBorder.Rounded : BoxBorder.Ascii,
        FencedCodeBlockInfoPanelBorder = emoji ? BoxBorder.Rounded : BoxBorder.Ascii,
        TableBorderStyle = new Style(foreground: Color.Grey35),
        Emojis = emoji,

        // SmartyPants turns "-f" into a typographic dash, in pages whose entire job is showing exact syntax a user has to type back.
        SmartyPants = false,

        // OSC 8 hyperlinks are invisible to ConsoleInteractive's width accounting and to its recent-message ring, which reconstructs what a popup covered from what was written.
        UseTerminalHyperlinks = false,
    };

    /// <summary>
    /// Maps MCC's configured colour depth onto Spectre's.
    /// One setting, one source of truth: a page must not emit truecolor into a terminal the rest of the client is treating as 4-bit.
    /// </summary>
    private static ColorSystem ToColorSystem(ConsoleColorDepth depth) => depth switch
    {
        ConsoleColorDepth.Disable => ColorSystem.NoColors,
        ConsoleColorDepth.Legacy4Bit or ConsoleColorDepth.Vt1004Bit => ColorSystem.Legacy,
        ConsoleColorDepth.Vt1008Bit => ColorSystem.EightBit,
        _ => ColorSystem.TrueColor,
    };
}
