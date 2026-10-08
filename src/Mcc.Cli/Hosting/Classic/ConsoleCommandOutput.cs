using Mcc.Cli.Presentation;
using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Hosting.Classic;

/// <summary>
/// An <see cref="ICommandOutput"/> that writes command body lines to the console, rendering the legacy section-sign colour codes the command text carries.
/// <para>
/// Commands emit legacy-coded text because that is what the corpus holds: the legacy client formatted its listings with codes like the trailing <c>§8</c> after a container title, and printed them through ConsoleIO.WriteLineFormatted. The new host had no section-code path at all, so those codes reached the terminal as the literal characters "§8".
/// Parsing with UMPK's LegacyText and rendering through the same AnsiComponentRenderer the chat presenter uses also means the colour depth (and NO_COLOR) is honoured in one place: at Disable the codes are dropped rather than printed.
/// </para>
/// </summary>
internal sealed class ConsoleCommandOutput : ICommandOutput
{
    public ConsoleCommandOutput(AnsiComponentRenderer renderer, ConsoleColorDepth depth)
        => HostConsole.UseRenderer(renderer, depth);

    // Through HostConsole, never raw Console: command output is the highest-volume line source in the classic host, so writing it outside ConsoleWriter is what drifts the suggestion popup's background ring the furthest.
    // See HostConsole for the mechanism.
    public void WriteLine(string text) => HostConsole.WriteFormatted(text);
}
