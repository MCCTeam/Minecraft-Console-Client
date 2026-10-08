using System.Text;
using Umpk.Text;

namespace Mcc.Cli.Presentation;

/// <summary>
/// The classic-host renderer that walks a UMPK <see cref="Component"/> tree and emits it as a single string, with ANSI (VT100) escape sequences for color and decorations when color is enabled, or clean plain text when it is not.
/// This replaces the legacy ColorHelper / TUI McColorParser for the classic host.
/// <para>
/// The walk itself (style inheritance, translation-key substitution, legacy section-sign decoding) is <see cref="ComponentFlattener"/>'s job now; this type is only the sink that turns each resolved <see cref="StyledRun"/> into SGR-escaped or plain text.
/// Translation keys (<see cref="TranslatableContent"/>) are resolved through the core translation source, keeping styled argument substitution.
/// The SGR shape itself (24-bit, 8-bit, or nearest-4-bit) is chosen per <see cref="ConsoleColorDepth"/> by <see cref="AnsiColorMapping"/>.
/// </para>
/// </summary>
internal sealed class AnsiComponentRenderer
{
    private const string Reset = "\u001b[0m";

    /// <summary>The control sequence introducer every SGR sequence opens with.</summary>
    private const string Csi = "\u001b[";

    private const string BoldCode = "1";
    private const string ItalicCode = "3";
    private const string UnderlineCode = "4";
    private const string StrikethroughCode = "9";

    private readonly ITranslationSource _translations;
    private readonly ConsoleColorDepth _depth;

    /// <summary>Creates a renderer over the given translation source; <paramref name="depth"/> gates and shapes ANSI output.</summary>
    public AnsiComponentRenderer(ITranslationSource translations, ConsoleColorDepth depth)
    {
        ArgumentNullException.ThrowIfNull(translations);
        _translations = translations;
        _depth = depth;
    }

    /// <summary>Whether this renderer emits ANSI escape sequences.</summary>
    public bool ColorEnabled => _depth != ConsoleColorDepth.Disable;

    /// <summary>Renders a component to a display string (ANSI-colored when enabled, plain otherwise).</summary>
    public string Render(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        var sb = new StringBuilder();
        ComponentFlattener.Flatten(component, new AnsiSink(sb, _depth), ComponentFlattenOptions.For(_translations));
        return sb.ToString();
    }

    // Emits one leaf run: the style's SGR sequence, the text, then a reset (when color is on and the style is not empty); plain text otherwise.
    // Reference-semantic (wraps the StringBuilder), so copying the struct through ComponentFlattener's recursive walk is safe.
    private readonly struct AnsiSink(StringBuilder sb, ConsoleColorDepth depth) : IStyledRunSink
    {
        public void Accept(in StyledRun run)
        {
            if (run.Text.Length == 0)
                return;

            if (depth == ConsoleColorDepth.Disable)
            {
                sb.Append(run.Text);
                return;
            }

            if (!AppendSgr(sb, run.Style, depth))
            {
                sb.Append(run.Text);
                return;
            }

            sb.Append(run.Text).Append(Reset);
        }
    }

    /// <summary>
    /// Writes a run's style as SGR sequences and reports whether it wrote any.
    /// The colour and each decoration go out as SEPARATE sequences rather than as one semicolon-joined parameter list.
    /// <para>
    /// Both forms paint identically in every terminal, and the compact one would be the obvious choice.
    /// This started as a workaround: the suggestion popup rebuilds whatever it covered from a ring of recent lines, and it used to classify every escape with six anchored COLOUR-ONLY regexes and discard anything else, so a combined <c>ESC[38;2;255;255;85;1m</c> was thrown away whole and the covered text came back with no colour.
    /// Reported on a server whose MOTD is a bold gradient, where every character carries such a pair.
    /// </para>
    /// <para>
    /// The submodule keeps every SGR escape now (ConsoleSuggestion.RecentMessage.ParseColorCode), so the split is no longer load-bearing.
    /// It stays because it costs nothing and it keeps this output readable by a consumer that has not taken that fix; nothing here depends on it any more.
    /// </para>
    /// </summary>
    private static bool AppendSgr(StringBuilder sb, Style style, ConsoleColorDepth depth)
    {
        bool wrote = false;

        if (style.Color is TextColor color)
        {
            string colorCode = AnsiColorMapping.Sgr(color, depth);
            if (colorCode.Length > 0)
            {
                sb.Append(Csi).Append(colorCode).Append('m');
                wrote = true;
            }
        }

        wrote |= AppendCode(sb, style.Bold == true, BoldCode);
        wrote |= AppendCode(sb, style.Italic == true, ItalicCode);
        wrote |= AppendCode(sb, style.Underlined == true, UnderlineCode);
        wrote |= AppendCode(sb, style.Strikethrough == true, StrikethroughCode);

        // Obfuscated has no faithful ANSI equivalent (vanilla scrambles glyphs); the run renders visibly.
        return wrote;
    }

    private static bool AppendCode(StringBuilder sb, bool on, string code)
    {
        if (!on)
            return false;

        sb.Append(Csi).Append(code).Append('m');
        return true;
    }
}
