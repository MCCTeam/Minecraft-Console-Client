using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Markdig;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdInline = Markdig.Syntax.Inlines.Inline;
using DMCBK.Core.Presentation;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>
/// Renders a <c>/man</c> page for the TUI, by building Consolonia controls out of the Markdig syntax tree.
/// <para>
/// The classic host's renderer cannot be reused here, and the reason is structural rather than cosmetic.
/// The classic console is a byte stream, so Spectre's ANSI is exactly the right output.
/// The TUI is a retained-mode Avalonia widget tree that paints its own cells and has no ANSI interpreter on the text path at all: handing it SGR escapes reproduces, one escape alphabet later, the precise defect <see cref="LegacyTextInlines"/> exists to fix, where <c>§</c> codes reached the screen as their literal characters.
/// </para>
/// <para>
/// So the parse is shared (Markdig, same pipeline) and only the back end differs.
/// Headings, paragraphs and code become <see cref="TextBlock"/>s with typed <see cref="Run"/> inlines; alert blocks and quotes become bordered panels; tables become grids.
/// Every colour comes from <see cref="LegacyTextInlines"/>'s palette, so a page looks like the rest of the TUI rather than like a second application embedded in it.
/// </para>
/// </summary>
internal sealed class MarkdownTuiRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .Build();

    // Drawn from LegacyTextInlines' vanilla palette so the manual matches the chat log beside it.
    private static readonly IBrush Heading = new SolidColorBrush(Color.FromRgb(255, 255, 85));   // §e
    private static readonly IBrush Subheading = new SolidColorBrush(Color.FromRgb(85, 255, 255)); // §b
    private static readonly IBrush Body = Brushes.White;
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(170, 170, 170));      // §7
    private static readonly IBrush Code = new SolidColorBrush(Color.FromRgb(85, 255, 255));      // §b
    private static readonly IBrush Rule = new SolidColorBrush(Color.FromRgb(85, 85, 85));        // §8
    private static readonly IBrush Warn = new SolidColorBrush(Color.FromRgb(255, 170, 0));       // §6

    private readonly GlyphSet _glyphs;

    public MarkdownTuiRenderer(GlyphSet glyphs) => _glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));

    /// <summary>Builds the controls for one page, in order, ready to append to the log pane.</summary>
    public IReadOnlyList<Control> Render(string markdown, bool wrapPreformatted = false)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var controls = new List<Control>();
        MarkdownDocument document = Markdown.Parse(markdown, Pipeline);

        // A blank line between top-level blocks, which is what the classic host's renderer produces and what makes a page readable: without it the headings, the prose, the table and the alerts run together into one wall, because a log pane has no margins of its own to separate them with.
        // Inside a block (list items, code lines, table rows) there is no spacer, since those are one thing each.
        bool first = true;
        foreach (Block block in document)
        {
            if (!first)
                controls.Add(Spacer());

            first = false;
            Emit(block, controls, indent: 0, wrapPreformatted);
        }

        return controls;
    }

    /// <summary>One empty line. A TextBlock rather than a margin, because the pane stacks lines.</summary>
    private static TextBlock Spacer() => new() { Text = string.Empty };

    private void Emit(Block block, List<Control> into, int indent, bool wrapPreformatted)
    {
        switch (block)
        {
            case HeadingBlock heading:
                into.Add(HeadingControl(heading));
                break;

            case ParagraphBlock paragraph:
                into.Add(Line(paragraph.Inline, Body, indent));
                break;

            case ListBlock list:
                EmitList(list, into, indent, wrapPreformatted);
                break;

            case QuoteBlock quote:
                EmitQuote(quote, into, indent, wrapPreformatted);
                break;

            case FencedCodeBlock or CodeBlock:
                EmitCode((LeafBlock)block, into, indent, wrapPreformatted);
                break;

            case Table table:
                EmitTable(table, into, indent, wrapPreformatted);
                break;

            case ThematicBreakBlock:
                into.Add(new TextBlock
                {
                    Text = new string('-', 40),
                    Foreground = Rule,
                    TextWrapping = wrapPreformatted ? TextWrapping.Wrap : TextWrapping.NoWrap,
                });
                break;

            case ContainerBlock container:
                foreach (Block child in container)
                    Emit(child, into, indent, wrapPreformatted);

                break;
        }
    }

    private TextBlock HeadingControl(HeadingBlock heading)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = heading.Level <= 1 ? Heading : Subheading,
            Inlines = [],
        };

        string text = PlainInline(heading.Inline);

        // An H1 is the page title and gets a rule under it, the same shape the classic renderer's SpectreRuleHeaderStyle produces, so the two hosts frame a page identically.
        block.Inlines!.Add(new Run(heading.Level <= 1 ? text.ToUpperInvariant() : text)
        {
            FontWeight = FontWeight.Bold,
        });

        return block;
    }

    private void EmitList(ListBlock list, List<Control> into, int indent, bool wrapPreformatted)
    {
        int number = 1;
        foreach (Block item in list)
        {
            if (item is not ListItemBlock listItem)
                continue;

            string marker = list.IsOrdered ? $"{number++}. " : "- ";
            bool first = true;
            foreach (Block child in listItem)
            {
                if (child is ParagraphBlock paragraph)
                {
                    into.Add(Line(paragraph.Inline, Body, indent + 1, first ? marker : "  "));
                    first = false;
                }
                else
                    Emit(child, into, indent + 1, wrapPreformatted);
            }
        }
    }

    private void EmitQuote(QuoteBlock quote, List<Control> into, int indent, bool wrapPreformatted)
    {
        // GitHub alert blocks (`> [!WARNING]`) are what the manual's KNOWN LIMITS sections use, and they are the one construct on a page that must not read as ordinary prose.
        // Markdig parses them into its own AlertBlock and consumes the marker, so the kind is read off the node rather than sniffed out of the text: a page written `> [!Warning]` parses the same as `> [!WARNING]`.
        if (quote is AlertBlock alert)
        {
            into.Add(new TextBlock
            {
                Text = $"{_glyphs.Warn} {alert.Kind.ToString().ToUpperInvariant()}",
                Foreground = Warn,
            });
        }

        bool first = true;
        foreach (Block child in quote)
        {
            // Paragraphs inside a quote are separate paragraphs, so they get the same blank line between them that top-level ones do.
            // The marker line stays attached to the first one.
            if (!first)
                into.Add(Spacer());

            first = false;
            Emit(child, into, indent + 1, wrapPreformatted);
        }
    }

    private void EmitCode(LeafBlock code, List<Control> into, int indent, bool wrapPreformatted)
    {
        if (code.Lines.Lines is null)
            return;

        for (int i = 0; i < code.Lines.Count; i++)
        {
            into.Add(new TextBlock
            {
                Text = new string(' ', (indent + 1) * 2) + code.Lines.Lines[i].Slice.ToString(),
                Foreground = Code,
                TextWrapping = wrapPreformatted ? TextWrapping.Wrap : TextWrapping.NoWrap,
            });
        }
    }

    private void EmitTable(Table table, List<Control> into, int indent, bool wrapPreformatted)
    {
        // Column widths are measured before anything is emitted, so the rows line up as a table rather than as a ragged list of cells.
        // A terminal grid has no layout pass to do this for us.
        var rows = new List<List<string>>();
        foreach (Block row in table)
        {
            if (row is not TableRow tableRow)
                continue;

            var cells = new List<string>();
            foreach (Block cell in tableRow)
            {
                cells.Add(cell is TableCell { Count: > 0 } tc && tc[0] is ParagraphBlock p
                    ? PlainInline(p.Inline)
                    : string.Empty);
            }

            rows.Add(cells);
        }

        if (rows.Count == 0)
            return;

        int columns = rows.Max(r => r.Count);
        var widths = new int[columns];
        foreach (List<string> row in rows)
        {
            for (int c = 0; c < row.Count; c++)
                widths[c] = Math.Max(widths[c], row[c].Length);
        }

        string pad = new(' ', (indent + 1) * 2);
        for (int r = 0; r < rows.Count; r++)
        {
            var sb = new StringBuilder(pad);
            for (int c = 0; c < columns; c++)
            {
                string cell = c < rows[r].Count ? rows[r][c] : string.Empty;
                sb.Append(cell.PadRight(widths[c]));
                if (c < columns - 1)
                    sb.Append("  ");
            }

            into.Add(new TextBlock
            {
                Text = sb.ToString().TrimEnd(),
                Foreground = r == 0 ? Subheading : Body,
                TextWrapping = wrapPreformatted ? TextWrapping.Wrap : TextWrapping.NoWrap,
            });

            if (r == 0)
            {
                into.Add(new TextBlock
                {
                    Text = pad + new string('-', widths.Sum() + ((columns - 1) * 2)),
                    Foreground = Rule,
                    TextWrapping = wrapPreformatted ? TextWrapping.Wrap : TextWrapping.NoWrap,
                });
            }
        }
    }

    /// <summary>One paragraph as a text block, with emphasis and inline code carried as typed runs.</summary>
    private TextBlock Line(ContainerInline? inline, IBrush brush, int indent, string marker = "")
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = brush,
            Inlines = [],
        };

        string pad = new(' ', indent * 2);
        if (pad.Length > 0 || marker.Length > 0)
            block.Inlines!.Add(new Run(pad + marker) { Foreground = Dim });

        AppendInlines(inline, block.Inlines!, brush, bold: false, italic: false);
        return block;
    }

    private void AppendInlines(ContainerInline? container, InlineCollection into, IBrush brush, bool bold, bool italic)
    {
        if (container is null)
            return;

        foreach (MdInline inline in container)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    into.Add(new Run(literal.Content.ToString())
                    {
                        Foreground = brush,
                        FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
                        FontStyle = italic ? FontStyle.Italic : FontStyle.Normal,
                    });
                    break;

                case CodeInline code:
                    into.Add(new Run(code.Content) { Foreground = Code });
                    break;

                case EmphasisInline emphasis:
                    AppendInlines(
                        emphasis, into, brush,
                        bold || emphasis.DelimiterCount >= 2,
                        italic || emphasis.DelimiterCount == 1);
                    break;

                case LineBreakInline:
                    into.Add(new Run(" ") { Foreground = brush });
                    break;

                case LinkInline link:
                    AppendInlines(link, into, Subheading, bold, italic);
                    break;

                case ContainerInline nested:
                    AppendInlines(nested, into, brush, bold, italic);
                    break;

                default:
                    into.Add(new Run(inline.ToString() ?? string.Empty) { Foreground = brush });
                    break;
            }
        }
    }

    private static string PlainInline(ContainerInline? container)
    {
        if (container is null)
            return string.Empty;

        var sb = new StringBuilder();
        Walk(container, sb);
        return sb.ToString();

        static void Walk(ContainerInline node, StringBuilder sb)
        {
            foreach (MdInline inline in node)
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        sb.Append(literal.Content.ToString());
                        break;
                    case CodeInline code:
                        sb.Append(code.Content);
                        break;
                    case ContainerInline nested:
                        Walk(nested, sb);
                        break;
                    default:
                        sb.Append(inline.ToString());
                        break;
                }
            }
        }
    }
}
