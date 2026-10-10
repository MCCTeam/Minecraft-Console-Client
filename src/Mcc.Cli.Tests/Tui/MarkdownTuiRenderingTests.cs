using Mcc.Cli.Presentation;
using Mcc.Cli.Tui.Presentation;
using Mcc.Cli;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Mcc.Cli.Tui;
using DMCBK.Core.Manual;
using DMCBK.Core.Presentation;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// The TUI's <c>/man</c> renderer.
/// <para>
/// The classic host's renderer cannot serve the TUI, and the reason is structural.
/// The classic console is a byte stream, so Spectre's ANSI is the right output for it.
/// The TUI is a retained-mode widget tree that paints its own cells and has no ANSI interpreter on the text path, so handing it escape sequences reproduces the defect <c>LegacyTextInlines</c> was written to fix, where section codes reached the screen as their literal characters.
/// These assert the TUI gets CONTROLS, and that no escape sequence survives into one.
/// </para>
/// </summary>
public sealed class MarkdownTuiRenderingTests
{
    private const string Page = """
        # Movement

        MCC runs a **local** physics simulation with `-f` available.

        ## Commands

        - walk
        - path

        | Key | Default |
        | --- | --- |
        | `Terrain` | true |

        > [!WARNING]
        > No flow field.
        """;

    private static MarkdownTuiRenderer Renderer(bool emoji = true)
        => new(emoji ? MccGlyphs.Emoji : GlyphSet.Ascii);

    private static string AllText(IEnumerable<Control> controls)
    {
        var sb = new System.Text.StringBuilder();
        foreach (Control control in controls)
        {
            if (control is not TextBlock block)
                continue;

            if (block.Inlines is { Count: > 0 } inlines)
            {
                foreach (Inline inline in inlines)
                {
                    if (inline is Run run)
                        sb.Append(run.Text);
                }
            }
            else
                sb.Append(block.Text);

            sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>The renderer produces controls, not a string.</summary>
    [Fact]
    public void ProducesControls()
    {
        IReadOnlyList<Control> controls = Renderer().Render(Page);

        Assert.NotEmpty(controls);
        Assert.All(controls, c => Assert.IsAssignableFrom<Control>(c));
    }

    /// <summary>The page's content survives into the controls.</summary>
    [Fact]
    public void KeepsTheContent()
    {
        string text = AllText(Renderer().Render(Page));

        Assert.Contains("MOVEMENT", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("physics simulation", text, StringComparison.Ordinal);
        Assert.Contains("Terrain", text, StringComparison.Ordinal);
        Assert.Contains("walk", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// The whole point: not one ANSI escape reaches a control.
    /// Consolonia would paint it as literal characters.
    /// </summary>
    [Fact]
    public void EmitsNoAnsiEscapes()
    {
        string text = AllText(Renderer().Render(Page));

        Assert.DoesNotContain('', text);
        Assert.DoesNotContain('§', text);
    }

    /// <summary>Markdown syntax is rendered, not printed.</summary>
    [Fact]
    public void DoesNotPrintMarkdownMarkers()
    {
        string text = AllText(Renderer().Render(Page));

        Assert.DoesNotContain("**", text, StringComparison.Ordinal);
        Assert.DoesNotContain("# ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("[!WARNING]", text, StringComparison.Ordinal);
    }

    /// <summary>Emphasis becomes a typed run rather than surviving as asterisks.</summary>
    [Fact]
    public void EmphasisBecomesATypedRun()
    {
        IReadOnlyList<Control> controls = Renderer().Render("Some **bold** text.\n");

        TextBlock block = Assert.IsType<TextBlock>(controls.Single());
        Assert.NotNull(block.Inlines);
        Assert.Contains(block.Inlines!, i => i is Run { Text: "bold" } run
                                             && run.FontWeight == Avalonia.Media.FontWeight.Bold);
    }

    /// <summary>An alert block is marked, because it is the one construct that must not read as prose.</summary>
    [Fact]
    public void AlertBlocksAreMarked()
    {
        string emoji = AllText(Renderer().Render("> [!WARNING]\n> Careful.\n"));
        Assert.Contains("WARNING", emoji, StringComparison.Ordinal);
        Assert.Contains("Careful.", emoji, StringComparison.Ordinal);

        // And the marker follows the glyph setting, like every other status glyph.
        string ascii = AllText(Renderer(emoji: false).Render("> [!WARNING]\n> Careful.\n"));
        Assert.Contains("[!]", ascii, StringComparison.Ordinal);
    }

    /// <summary>A table lines up, because a terminal grid has no layout pass to do it for us.</summary>
    [Fact]
    public void TableColumnsAlign()
    {
        IReadOnlyList<Control> controls =
            Renderer().Render("| Key | Default |\n| --- | --- |\n| A | 1 |\n| LongerKey | 2 |\n");

        string[] rows = [.. AllText(controls).Split('\n', StringSplitOptions.RemoveEmptyEntries)];
        Assert.True(rows.Length >= 3);

        // Every data row starts its second column at the same offset.
        int first = rows[^2].IndexOf('1', StringComparison.Ordinal);
        int second = rows[^1].IndexOf('2', StringComparison.Ordinal);
        Assert.Equal(first, second);
    }

    [Fact]
    public void BrowserRenderingCanWrapCodeAndTablesForVerticalOnlyScrolling()
    {
        const string page = "```text\na very long code line\n```\n\n| Key | Value |\n| --- | --- |\n| Long | Row |";

        IReadOnlyList<Control> controls = Renderer().Render(page, wrapPreformatted: true);

        Assert.All(
            controls.OfType<TextBlock>().Where(block => !string.IsNullOrEmpty(block.Text)),
            block => Assert.Equal(Avalonia.Media.TextWrapping.Wrap, block.TextWrapping));
    }

    /// <summary>
    /// Top-level blocks are separated by a blank line, which is what the classic host's renderer produces.
    /// Without it a page ran together into one wall: the log pane stacks lines and has no margins of its own to separate a heading from the paragraph under it.
    /// </summary>
    [Fact]
    public void BlocksAreSeparatedByBlankLines()
    {
        IReadOnlyList<Control> controls = Renderer().Render("# Title\n\nFirst.\n\nSecond.\n");

        static bool IsBlank(Control c)
            => c is TextBlock { Inlines: null or { Count: 0 } } b && string.IsNullOrEmpty(b.Text);

        Assert.Equal(2, controls.Count(IsBlank));

        // And they sit BETWEEN blocks, never at the start.
        Assert.False(IsBlank(controls[0]));
    }

    /// <summary>Rows inside one block are not separated; a list is one thing, not three.</summary>
    [Fact]
    public void RowsInsideABlockAreNotSeparated()
    {
        IReadOnlyList<Control> controls = Renderer().Render("- one\n- two\n- three\n");

        Assert.Equal(3, controls.Count);
        Assert.DoesNotContain(controls, c => c is TextBlock b
                                             && string.IsNullOrEmpty(b.Text)
                                             && b.Inlines is null or { Count: 0 });
    }

    /// <summary>Every shipped page renders into controls without throwing.</summary>
    [Fact]
    public void EveryShippedPageRenders()
    {
        MarkdownTuiRenderer renderer = Renderer();
        foreach (ManualTopic topic in ManualTopics.All)
        {
            IReadOnlyList<Control> controls = renderer.Render(new ManualCatalog().Read(topic.Id)!);
            Assert.NotEmpty(controls);
        }
    }
}
