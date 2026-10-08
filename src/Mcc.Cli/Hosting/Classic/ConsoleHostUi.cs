using Mcc.Cli.Commands;
using Mcc.Cli.Presentation;
using DMCBK.Core.Commands;

namespace Mcc.Cli.Hosting.Classic;

/// <summary>
/// The classic-console host UI.
/// It has no rich views yet (those arrive with the TUI, so the container / book / tab / dialog hooks stay text-fallback), but it does implement the plugin notification path (<see cref="IHostUi.TryNotify"/>): a plugin's alert becomes an optional console beep plus a written line.
/// Plugins that reach for a notification therefore get a real host channel instead of touching the console.
/// </summary>
internal sealed class ConsoleHostUi : IHostUi, IMccNavigation
{
    public string? FormatCommandIndex(IReadOnlyList<CommandBase> commands, string prefix, DMCBK.Core.Presentation.GlyphSet glyphs)
        => Mcc.Cli.Presentation.TerminalHelpRenderer.Index(commands, prefix,
            new Mcc.Cli.Presentation.TerminalHelpRenderer.GlyphContext(glyphs.Ok, glyphs.Fail));

    public string? FormatCommandHelp(CommandHelpPresentation request, DMCBK.Core.Presentation.GlyphSet glyphs)
        => Mcc.Cli.Presentation.TerminalHelpRenderer.RenderPage(request, glyphs);

    public bool ShowInventoryLayout { get; set; } = true;

    public bool TryPresentChunkMap(ChunkMapPresentation request, DMCBK.Core.Presentation.GlyphSet glyphs)
    {
        foreach (string line in Mcc.Cli.Presentation.ChunkMapRenderer.Render(request, glyphs))
            HostConsole.WriteLine(line);
        return true;
    }

    public string? FormatPlayerStatus(DMCBK.Core.PlayerStatus status, DMCBK.Core.Presentation.GlyphSet glyphs)
        => Mcc.Cli.Presentation.PlayerStatusRenderer.Render(status, glyphs);

    public string? FormatProgress(double fraction, DMCBK.Core.Presentation.GlyphSet glyphs)
        => "`" + Mcc.Cli.Presentation.Meter.Render(fraction, 15, glyphs.IsEmoji) + "` " + Mcc.Cli.Presentation.Meter.Percent(fraction);

    public string? GetInventoryLayout(string? menuType, bool playerInventory)
        => !ShowInventoryLayout ? null : playerInventory ? Mcc.Cli.Localization.ContainerArt.PlayerInventory : Mcc.Cli.Localization.ContainerArt.ForMenuType(menuType);

    public bool TryOpenMccMenu() => false;

    /// <summary>
    /// The Markdown renderer for <c>/man</c>, installed once startup knows the colour depth and the glyph set.
    /// Null until then, and null forever in a host that never got one, in which case the manual falls back to plain text.
    /// </summary>
    public MarkdownConsoleRenderer? Documents { get; set; }

    /// <inheritdoc/>
    public bool TryWriteDocument(string markdown)
    {
        if (Documents is not { } renderer || string.IsNullOrEmpty(markdown))
            return false;

        renderer.Write(markdown);
        return true;
    }

    /// <inheritdoc/>
    public bool TryPresentImage(ImagePresentationRequest request)
    {
        int width = TryWindowWidth() ?? 80;
        int height = TryWindowHeight() ?? 40;
        int scale = Presentation.ConsoleMapRenderer.ComputeScale(request.Image.Width, request.Image.Height, width, height);
        foreach (string line in Presentation.ConsoleMapRenderer.Render(request.Image, scale))
            HostConsole.WriteLine(line);
        return true;
    }

    // The chunk map wants an odd cell count so the player's chunk sits at the exact center.
    private const int MinExtent = 5;
    private const int MaxColumns = 63;
    private const int MaxRows = 41;

    /// <inheritdoc/>
    // Re-sources the legacy Console.BufferWidth read (Chunk.cs:105-106) that was lifted out of core: the host supplies the real terminal width, leaving a small margin, as an odd column count.
    // Null when there is no interactive console (redirected output), so the chunk command falls back to its own default grid.
    public int? ChunkViewportColumns => Viewport(TryWindowWidth, margin: 2, max: MaxColumns);

    /// <inheritdoc/>
    // Re-sources the legacy Console.BufferHeight read: the real terminal height, leaving room for the input line and command output, as an odd row count.
    // Null when output is redirected.
    public int? ChunkViewportRows => Viewport(TryWindowHeight, margin: 6, max: MaxRows);

    /// <inheritdoc/>
    public bool TryNotify(HostNotification notification)
    {
        if (notification is null)
            return false;

        if (notification.Beep)
        {
            try
            {
                Console.Beep();
            }
            catch (Exception ex) when (ex is PlatformNotSupportedException or System.IO.IOException)
            {
                // Not every console/platform supports Beep; the written line is the fallback.
            }
        }

        HostConsole.WriteLine(notification.Message);
        return true;
    }

    private static int? Viewport(Func<int?> read, int margin, int max)
    {
        int? size = read();
        if (size is not { } value || value <= 0)
            return null;

        int extent = Math.Clamp(value - margin, MinExtent, max);
        if (extent % 2 == 0)
            extent--;

        return extent < MinExtent ? MinExtent : extent;
    }

    private static int? TryWindowWidth()
    {
        try
        {
            return Console.IsOutputRedirected ? null : Console.WindowWidth;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static int? TryWindowHeight()
    {
        try
        {
            return Console.IsOutputRedirected ? null : Console.WindowHeight;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
