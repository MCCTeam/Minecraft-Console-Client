using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>
/// Renders legacy section-sign coded text into a Consolonia <see cref="TextBlock"/>.
/// <para>
/// The TUI had no section-code path at all, so lines the commands emit with codes reached the screen as their literal characters.
/// It showed up most obviously in the chunk map, whose player and marked cells are a pair of spaces wrapped in background codes: the legend read <c>Player:§§7 §§r, MarkedChunk:§§4 §§r</c> with the codes visible and the cells themselves therefore blank and unmarked.
/// </para>
/// <para>
/// The DOUBLE sign is the important part and is why UMPK's <c>LegacyText</c> is not enough here.
/// <c>§X</c> is vanilla's foreground code; <c>§§X</c> is MCC's own BACKGROUND extension, understood by the ConsoleInteractive writer the classic host uses (its format regex admits <c>§§[0-9a-fr]</c>) but by nothing in vanilla.
/// The legacy TUI's own McColorParser did not implement it either, so this is deliberately one better than legacy rather than a copy of it: a straight port would have left the chunk map's two marker cells invisible in the TUI, which is the defect being fixed.
/// </para>
/// </summary>
internal static class LegacyTextInlines
{
    /// <summary>Vanilla's sixteen chat colours (MinecraftClient/Tui/McColorParser.cs:15-33).</summary>
    private static readonly Dictionary<char, IBrush> Palette = new()
    {
        ['0'] = Brush(0, 0, 0),
        ['1'] = Brush(0, 0, 170),
        ['2'] = Brush(0, 170, 0),
        ['3'] = Brush(0, 170, 170),
        ['4'] = Brush(170, 0, 0),
        ['5'] = Brush(170, 0, 170),
        ['6'] = Brush(255, 170, 0),
        ['7'] = Brush(170, 170, 170),
        ['8'] = Brush(85, 85, 85),
        ['9'] = Brush(85, 85, 255),
        ['a'] = Brush(85, 255, 85),
        ['b'] = Brush(85, 255, 255),
        ['c'] = Brush(255, 85, 85),
        ['d'] = Brush(255, 85, 255),
        ['e'] = Brush(255, 255, 85),
        ['f'] = Brushes.White,
    };

    private static SolidColorBrush Brush(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));

    /// <summary>True when the text carries at least one code worth parsing.</summary>
    public static bool HasCodes(string? text) => text is not null && text.Contains('§', StringComparison.Ordinal);

    /// <summary>
    /// Builds a text block for a legacy-coded line.
    /// Text with no codes gets the plain fast path, so the overwhelmingly common case (chat, plain log lines) costs nothing.
    /// </summary>
    public static TextBlock Render(string text, TextWrapping wrapping = TextWrapping.Wrap)
    {
        var block = new TextBlock
        {
            TextWrapping = wrapping,
            Background = Brushes.Black,
            Foreground = Brushes.White,
        };

        if (!HasCodes(text))
        {
            block.Text = text ?? string.Empty;
            return block;
        }

        var state = new RunState();
        int start = 0;
        int i = 0;

        while (i < text.Length)
        {
            if (text[i] != '§' || i + 1 >= text.Length)
            {
                i++;
                continue;
            }

            // A background code is TWO signs then the colour, so it has to be tested before the single form or the second sign is consumed as an unknown foreground code and the colour character then leaks into the visible text (which is what "7 " on screen would have been).
            bool background = text[i + 1] == '§';
            int codeIndex = background ? i + 2 : i + 1;
            if (codeIndex >= text.Length)
            {
                i++;
                continue;
            }

            if (!TryReadCode(text, codeIndex, out IBrush? brush, out char code, out int consumed))
            {
                i++;
                continue;
            }

            Append(block, text[start..i], state);
            state.Apply(brush, code, background);
            i = codeIndex + consumed;
            start = i;
        }

        Append(block, text[start..], state);
        return block;
    }

    // Returns the brush for a colour code, or the raw code letter for a decoration/reset. `consumed` counts the characters after the sign(s), which is 1 for a plain code and 7 for the #rrggbb extension.
    private static bool TryReadCode(string text, int index, out IBrush? brush, out char code, out int consumed)
    {
        brush = null;
        code = char.ToLowerInvariant(text[index]);
        consumed = 1;

        if (code == '#' && index + 7 <= text.Length
            && uint.TryParse(text.AsSpan(index + 1, 6), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out uint rgb))
        {
            brush = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
            consumed = 7;
            return true;
        }

        if (Palette.TryGetValue(code, out IBrush? mapped))
        {
            brush = mapped;
            return true;
        }

        return code is 'l' or 'o' or 'n' or 'm' or 'r';
    }

    private static void Append(TextBlock block, string text, RunState state)
    {
        if (text.Length == 0)
            return;

        block.Inlines ??= [];

        TextDecorationCollection? decorations = null;
        if (state.Underline || state.Strikethrough)
        {
            decorations = [];
            if (state.Underline)
                decorations.Add(new TextDecoration { Location = TextDecorationLocation.Underline });

            if (state.Strikethrough)
                decorations.Add(new TextDecoration { Location = TextDecorationLocation.Strikethrough });
        }

        block.Inlines.Add(new Run(text)
        {
            Foreground = state.Foreground,
            Background = state.Background,
            FontWeight = state.Bold ? FontWeight.Bold : FontWeight.Normal,
            FontStyle = state.Italic ? FontStyle.Italic : FontStyle.Normal,
            TextDecorations = decorations,
        });
    }

    private sealed class RunState
    {
        public IBrush Foreground { get; private set; } = Brushes.White;

        public IBrush? Background { get; private set; }

        public bool Bold { get; private set; }

        public bool Italic { get; private set; }

        public bool Underline { get; private set; }

        public bool Strikethrough { get; private set; }

        public void Apply(IBrush? brush, char code, bool background)
        {
            if (brush is not null)
            {
                if (background)
                {
                    Background = brush;
                    return;
                }

                // Vanilla: a colour code clears the decorations with it.
                Foreground = brush;
                Bold = Italic = Underline = Strikethrough = false;
                return;
            }

            switch (code)
            {
                case 'l': Bold = true; break;
                case 'o': Italic = true; break;
                case 'n': Underline = true; break;
                case 'm': Strikethrough = true; break;
                case 'r':
                    // §§r resets only the background, so a foreground set before it survives; §r is the full vanilla reset.
                    if (background)
                        Background = null;
                    else
                    {
                        Foreground = Brushes.White;
                        Bold = Italic = Underline = Strikethrough = false;
                    }

                    break;
            }
        }
    }
}
