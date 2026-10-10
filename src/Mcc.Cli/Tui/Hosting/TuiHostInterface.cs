using Mcc.Cli.Tui.Authentication;
using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Auth;

namespace Mcc.Cli.Tui.Hosting;

/// <summary>
/// The TUI host interface handed to the client: no version-resolution prompt (an undetectable version fails fast, as in classic), a TUI-driven auth interaction (device-code and credential prompts surface in the TUI log/input), the TUI command-output sink, and the real <see cref="TuiHostUi"/> so container/book/tab/ dialog commands open Consolonia overlays instead of falling back to text.
/// </summary>
internal sealed class TuiHostInterface : IHostInterface
{
    public TuiHostInterface(TuiBackend backend, TuiHostUi ui)
    {
        AuthInteraction = new TuiAuthInteraction(backend);
        CommandOutput = new TuiCommandOutput(backend);
        ResourcePackPrompt = new TuiResourcePackPrompt(backend);
        Ui = ui;
    }

    public IUserPrompt? Prompt => null;

    public IAuthInteraction? AuthInteraction { get; }

    public ICommandOutput? CommandOutput { get; }

    public IHostUi? Ui { get; }

    public IResourcePackPrompt? ResourcePackPrompt { get; }
}
