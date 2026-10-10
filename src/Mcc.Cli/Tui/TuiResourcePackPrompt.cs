using Mcc.Cli.Hosting.Classic;
using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Authentication;
using Mcc.Cli.Tui.Hosting;
using DMCBK.Core;

namespace Mcc.Cli.Tui;

/// <summary>
/// The TUI answer to <see cref="IResourcePackPrompt"/>: the offer goes to the TUI log and the answer comes back from the input line, mirroring <see cref="TuiAuthInteraction"/>.
/// Answered yes/y accepts; anything else declines.
/// </summary>
internal sealed class TuiResourcePackPrompt(TuiBackend backend) : IResourcePackPrompt
{
    public async ValueTask<bool> PromptAsync(ResourcePackPromptRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        backend.WriteLine(Strings.ResourcePackQuestion(request.Url, request.Required));
        string line = await backend.RequestLineAsync(ct).ConfigureAwait(false);
        return ConsoleResourcePackPrompt.IsYes(line);
    }
}
