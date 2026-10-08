using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Hosting;

/// <summary>Mutable console-host state the host commands toggle (chat visibility).</summary>
internal sealed class ConsoleHostState
{
    public ConsoleHostState(bool chatVisible) => ChatVisible = chatVisible;

    public bool ChatVisible { get; set; }
}
