using Mcc.Cli.Presentation;
using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Auth;

namespace Mcc.Cli.Hosting.Classic;

/// <summary>
/// The classic-console host interface: no version-resolution prompt yet (an undetectable version fails fast with a typed error), a console-backed auth interaction drives interactive online logins, and a
/// console command-output sink renders command body text. No rich UI hooks yet (the TUI supplies those);
/// commands fall back to text.
/// </summary>
internal sealed class ConsoleHostInterface : IHostInterface
{
    public ConsoleHostInterface(AnsiComponentRenderer renderer, ConsoleColorDepth colorDepth)
    {
        CommandOutput = new ConsoleCommandOutput(renderer, colorDepth);
        AuthInteraction = new ConsoleAuthInteraction(colorDepth);
        ResourcePackPrompt = new ConsoleResourcePackPrompt();
    }

    public IUserPrompt? Prompt => null;

    public IAuthInteraction? AuthInteraction { get; }

    public ICommandOutput? CommandOutput { get; }

    public IHostUi? Ui { get; } = new ConsoleHostUi();

    public IResourcePackPrompt? ResourcePackPrompt { get; }
}
