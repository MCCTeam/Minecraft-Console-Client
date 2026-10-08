using Mcc.Cli.Hosting.Classic;
using DMCBK.Core.Commands;

namespace Mcc.Cli.Tui.Hosting;

/// <summary>Routes command body output to the TUI chat log (the TUI analogue of <see cref="ConsoleCommandOutput"/>).</summary>
internal sealed class TuiCommandOutput : ICommandOutput
{
    private readonly TuiBackend _backend;

    public TuiCommandOutput(TuiBackend backend) => _backend = backend;

    public void WriteLine(string text) => _backend.WriteLine(text);
}
