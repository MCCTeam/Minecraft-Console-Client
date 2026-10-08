using Mcc.Cli.Presentation;
using DMCBK.Core;
using DMCBK.Core.Commands;

namespace Mcc.Cli.Tui.Input;

/// <summary>
/// Computes the TUI input suggestions the same way the classic <see cref="RichConsole"/> reader does, because both now ask the same question of the same place: <see cref="CommandService.CompleteInputAsync(string, int, System.Threading.CancellationToken)"/> merges the internal-command tree with server command completion, applies the prefix rules (including the <c>//</c> server-command escape) once, and returns the replacement span already expressed in the raw input's own indices.
/// This wrapper only drops the tooltips, which the TUI popup does not render.
/// </summary>
internal static class TuiCompletion
{
    public static async Task<(IReadOnlyList<string> Suggestions, int Start, int End)> ComputeAsync(
        Client client, string text, int cursor)
    {
        InputCompletion completion = await client.Commands.CompleteInputAsync(text, cursor).ConfigureAwait(false);

        var ordered = new List<string>(completion.Suggestions.Count);
        foreach (ChatCompletion candidate in completion.Suggestions)
            ordered.Add(candidate.Text);

        return (ordered, completion.Start, completion.End);
    }
}
