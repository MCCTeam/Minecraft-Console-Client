using Mcc.Cli.Hosting;
using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using DMCBK.Core.Presentation;

namespace Mcc.Cli.Startup;

/// <summary>
/// The <c>--help</c> / <c>--help-short</c> one-shots.
/// Handled before everything in startup, ahead of the rich writer that clears the terminal, so the page survives and no configuration folder is read or generated on the way.
/// <para>
/// <c>--help</c> (and <c>-h</c>) renders the friendly Markdown guide for people who do not live in terminals.
/// <c>--help-short</c> prints the compact <see cref="Strings.Usage"/> reference for scripts and old habits.
/// Both print to stdout and exit clean.
/// </para>
/// </summary>
internal static class CliHelp
{
    internal const string HelpFlag = "--help";
    internal const string HelpShortFlag = "--help-short";
    internal const string HelpShorthand = "-h";

    /// <summary>
    /// Prints help when <paramref name="args"/> asks for it, and reports whether it did.
    /// False means this is an ordinary run and startup carries on.
    /// </summary>
    internal static bool TryHandle(string[] args, out int exitCode)
    {
        ArgumentNullException.ThrowIfNull(args);
        exitCode = HostExit.Clean;

        bool shortHelp = false;
        bool fullHelp = false;
        foreach (string arg in args)
        {
            if (string.Equals(arg, HelpShortFlag, StringComparison.Ordinal))
                shortHelp = true;
            else if (string.Equals(arg, HelpFlag, StringComparison.Ordinal)
                || string.Equals(arg, HelpShorthand, StringComparison.Ordinal)
                || string.Equals(arg, "-?", StringComparison.Ordinal)
                || string.Equals(arg, "/?", StringComparison.Ordinal))
                fullHelp = true;
        }

        if (!shortHelp && !fullHelp)
            return false;

        // The compact reference wins when both are present: it is the smaller promise.
        if (shortHelp)
        {
            Console.Out.WriteLine(Strings.Usage);
            return true;
        }

        ConsoleColorDepth depth = TerminalCapability.ResolveColorDepth(ConsoleColorDepth.Vt10024Bit);
        GlyphSet glyphs = GlyphModeResolver.DetectSupportsEmoji() ? MccGlyphs.Emoji : GlyphSet.Ascii;
        var renderer = new MarkdownConsoleRenderer(depth, glyphs);
        renderer.Write(Strings.HelpMarkdown(glyphs.IsEmoji));
        return true;
    }
}
