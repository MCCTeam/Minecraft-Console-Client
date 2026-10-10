using Mcc.Cli.Logging;
namespace Mcc.Cli.Presentation;

/// <summary>
/// The five ANSI color depths <c>console.toml</c>'s <c>ConsoleColorMode</c> can select, ported 1:1 from legacy's <c>Settings.ConsoleConfigHealper.ConsoleConfig.ConsoleColorModeType</c> (MinecraftClient/Settings.cs:1291).
/// Legacy's classic backend is the only consumer of this depth (its TUI used a separate color pipeline), matching the new client, where only the classic host's ANSI renderer (<see cref="AnsiComponentRenderer"/>) and log prefixes (<see cref="ConsoleLogger"/>) are depth-aware; the TUI keeps its own on/off color switch (<see cref="Tui.ComponentInlineRenderer"/>), since Consolonia paints true-color brushes directly rather than emitting SGR sequences of a chosen depth.
/// <para>
/// <c>Legacy4Bit</c> and <c>Vt1004Bit</c> are kept as distinct config values for parity with legacy's documented options, but this client is VT100-only (no <c>System.ConsoleColor</c> fallback path the way legacy's <c>legacy_4bit</c> used on Windows), so both resolve to the identical 4-bit ANSI SGR output here.
/// </para>
/// </summary>
internal enum ConsoleColorDepth
{
    /// <summary>No ANSI escapes at all.</summary>
    Disable,

    /// <summary>Nearest of the 16 basic ANSI colors, classic SGR 30-37/90-97 (legacy's Windows-console path has no equivalent here; see the type remarks).</summary>
    Legacy4Bit,

    /// <summary>Nearest of the 16 basic ANSI colors, classic SGR 30-37/90-97.</summary>
    Vt1004Bit,

    /// <summary>The xterm 256-color palette, SGR <c>38;5;N</c>.</summary>
    Vt1008Bit,

    /// <summary>24-bit truecolor, SGR <c>38;2;R;G;B</c>.</summary>
    Vt10024Bit,
}

/// <summary>
/// Decides whether, and at what depth, ANSI color should be emitted for this process, folding together the host config toggle (console.toml <c>ConsoleColorMode</c>), the <c>NO_COLOR</c> convention, and whether stdout is an interactive terminal.
/// A redirected stdout (files, pipes, the test harness) is treated as non-color by default so captured logs stay clean, but <c>MCC_FORCE_COLOR=1</c> forces color on (the live UX spread sets it so the ANSI path is exercised even when output is redirected to a log file).
/// <c>NO_COLOR</c> always wins.
/// </summary>
internal static class TerminalCapability
{
    /// <summary>
    /// Parses console.toml's <c>ConsoleColorMode</c> string (<c>disable</c>/<c>legacy_4bit</c>/<c>vt100_4bit</c>/ <c>vt100_8bit</c>/<c>vt100_24bit</c>, case-insensitive).
    /// An unrecognized value falls back to the documented default (<see cref="ConsoleColorDepth.Vt10024Bit"/>), the same lenient-fallback policy the host already applies to its other free-text enum-like settings (e.g. <c>ConsoleMode</c>).
    /// </summary>
    public static ConsoleColorDepth ParseColorDepth(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "disable" => ConsoleColorDepth.Disable,
        "legacy_4bit" => ConsoleColorDepth.Legacy4Bit,
        "vt100_4bit" => ConsoleColorDepth.Vt1004Bit,
        "vt100_8bit" => ConsoleColorDepth.Vt1008Bit,
        "vt100_24bit" => ConsoleColorDepth.Vt10024Bit,
        _ => ConsoleColorDepth.Vt10024Bit,
    };

    /// <summary>
    /// Resolves the effective color depth.
    /// Color is on (at <paramref name="configuredDepth"/>) only when it is not already <see cref="ConsoleColorDepth.Disable"/>, <c>NO_COLOR</c> is unset, and either stdout is an interactive terminal or <c>MCC_FORCE_COLOR=1</c>.
    /// </summary>
    public static ConsoleColorDepth ResolveColorDepth(ConsoleColorDepth configuredDepth)
    {
        if (configuredDepth == ConsoleColorDepth.Disable)
            return ConsoleColorDepth.Disable;

        if (Environment.GetEnvironmentVariable("NO_COLOR") is { Length: > 0 })
            return ConsoleColorDepth.Disable;

        if (string.Equals(Environment.GetEnvironmentVariable("MCC_FORCE_COLOR"), "1", StringComparison.Ordinal))
            return configuredDepth;

        try
        {
            return Console.IsOutputRedirected ? ConsoleColorDepth.Disable : configuredDepth;
        }
        catch (IOException)
        {
            return ConsoleColorDepth.Disable;
        }
    }

    /// <summary>
    /// Resolves the effective on/off color setting, for the callers that only need a boolean capability (the TUI's Consolonia color pipeline and the bootstrap logger before console.toml is known).
    /// Expressed in terms of <see cref="ResolveColorDepth"/> so the two never drift: on maps to the 24-bit depth, off to disabled, and the result is just whether the resolved depth is not disabled.
    /// </summary>
    public static bool ResolveColor(bool configEnabled)
        => ResolveColorDepth(configEnabled ? ConsoleColorDepth.Vt10024Bit : ConsoleColorDepth.Disable) != ConsoleColorDepth.Disable;
}
