using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using DMCBK.Core;

namespace Mcc.Cli.Hosting.Classic;

/// <summary>
/// The classic-console answer to <see cref="IResourcePackPrompt"/>: prints the server's offer through <see cref="HostConsole"/> (the only path that repaints the input area) and reads the answer off a pool thread, the same way <see cref="ConsoleAuthInteraction"/> reads.
/// Answered yes/y accepts; anything else, including an empty line, declines.
/// </summary>
internal sealed class ConsoleResourcePackPrompt : IResourcePackPrompt
{
    public async ValueTask<bool> PromptAsync(ResourcePackPromptRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        HostConsole.WriteLine(Strings.Host(Strings.ResourcePackQuestion(request.Url, request.Required)));
        string? line = await Task.Run(Console.ReadLine, ct).ConfigureAwait(false);
        return IsYes(line);
    }

    internal static bool IsYes(string? line)
        => line is not null
            && (line.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)
                || line.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase));
}
