using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Input;
using DMCBK.Core.Configuration;

namespace Mcc.Cli.Tui.Authentication;

/// <summary>The answered connect dialog: connect to a server, or exit the client.</summary>
/// <param name="InputLine">The full <c>/connect</c> line (prefix included), or an empty string for exit.</param>
/// <param name="ToSave">A servers.toml entry to persist first, or null for a one-off connect.</param>
/// <param name="ExitClient">True when the explicit Exit client action was picked.</param>
internal sealed record TuiConnectChoice(string InputLine, ConfiguredServer? ToSave, bool ExitClient = false);

/// <summary>
/// The TUI connect dialog, shown when the client reaches its idle prompt with no session: the saved servers and the picker actions first, then a new-server form (host, optional port, optional name) when New server is picked.
/// Quoted names round-trip through the same grammar typing would.
/// </summary>
internal static class TuiConnectDialog
{
    private const string HostKey = "host";
    private const string PortKey = "port";
    private const string NameKey = "name";

    /// <summary>
    /// Runs the dialog against the snapshot's server list.
    /// Returns null when it was dismissed.
    /// Cancellation throws <see cref="OperationCanceledException"/>.
    /// </summary>
    internal static async Task<TuiConnectChoice?> PickAsync(
        TuiBackend backend, DmcbkConfiguration config, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(backend);
        ArgumentNullException.ThrowIfNull(config);

        IReadOnlyList<ConfiguredServer> servers = config.Servers.Servers;
        bool showList = true;
        string? error = null;

        while (true)
        {
            if (showList)
            {
                int newServerIndex = servers.Count;
                int closeIndex = newServerIndex + 1;
                int exitIndex = closeIndex + 1;
                List<string> buttons =
                [
                    .. servers.Select(s => s.Name),
                    Strings.TuiConnectNewServer,
                    Strings.TuiPromptClose,
                    Strings.TuiConnectExitClient,
                ];
                List<TuiPromptButtonKind> buttonKinds =
                [
                    .. servers.Select(_ => TuiPromptButtonKind.Secondary),
                    TuiPromptButtonKind.Primary,
                    TuiPromptButtonKind.Quiet,
                    TuiPromptButtonKind.Danger,
                ];
                TuiPromptResult? pick = await TuiPrompt.AskAsync(
                    backend,
                    Strings.TuiConnectTitle,
                    [],
                    [],
                    buttons,
                    ct,
                    buttonKinds: buttonKinds,
                    stackedButtonCount: servers.Count).ConfigureAwait(false);
                if (pick is null || pick.ButtonIndex == closeIndex)
                    return null;

                if (pick.ButtonIndex == exitIndex)
                    return new TuiConnectChoice(string.Empty, null, ExitClient: true);

                if (pick.ButtonIndex < servers.Count)
                    return new TuiConnectChoice(ConnectLine(config, Quote(servers[pick.ButtonIndex].Name)), null);

                showList = false;
                error = null;
                continue;
            }

            TuiPromptResult? form = await TuiPrompt.AskAsync(
                backend,
                Strings.TuiConnectNewTitle,
                error is null ? [] : [error],
                [
                    new TuiPromptField(HostKey, Strings.TuiFieldHost),
                    new TuiPromptField(PortKey, Strings.TuiFieldPort),
                    new TuiPromptField(NameKey, Strings.TuiFieldServerName),
                ],
                [Strings.TuiConnectConnect, Strings.TuiConnectSaveConnect, Strings.TuiPromptBack],
                ct,
                buttonKinds:
                [
                    TuiPromptButtonKind.Primary,
                    TuiPromptButtonKind.Secondary,
                    TuiPromptButtonKind.Quiet,
                ]).ConfigureAwait(false);
            if (form is null || form.ButtonIndex == 2)
            {
                showList = true;
                error = null;
                continue;
            }

            string host = Value(form, HostKey).Trim();
            string portText = Value(form, PortKey).Trim();
            string name = Value(form, NameKey).Trim();
            if (host.Length == 0)
            {
                error = Strings.TuiConnectNeedsHost;
                continue;
            }

            ConfiguredServer? parsed = ParseLiteral(portText.Length == 0 ? host : $"{host}:{portText}");
            if (parsed is null)
            {
                error = Strings.TuiConnectBadAddress;
                continue;
            }

            // A one-off connect ignores the name; saving without one has nothing to file under.
            if (form.ButtonIndex == 0)
            {
                return new TuiConnectChoice(
                    ConnectLine(config, portText.Length == 0 ? host : $"{host}:{portText}"), null);
            }

            if (name.Length == 0)
            {
                error = Strings.TuiConnectNeedsName;
                continue;
            }

            var entry = new ConfiguredServer { Name = name, Host = parsed.Host, Port = parsed.Port };
            return new TuiConnectChoice(ConnectLine(config, Quote(name)), entry);
        }
    }

    private static string Value(TuiPromptResult form, string key)
        => form.Values.TryGetValue(key, out string? value) ? value : string.Empty;

    private static string Quote(string name)
        => name.Contains(' ') ? $"\"{name}\"" : name;

    /// <summary>The full input line for a connect argument, under the configured command prefix.</summary>
    private static string ConnectLine(DmcbkConfiguration config, string argument)
    {
        string prefix = config.Permissions.CommandPrefix switch
        {
            InternalCommandPrefix.None => string.Empty,
            InternalCommandPrefix.Backslash => "\\",
            _ => "/",
        };

        return $"{prefix}connect {argument}";
    }

    /// <summary>
    /// Parses a literal <c>host[:port]</c> (default 25565), accepting exactly what the connect command's literal branch accepts.
    /// Saved names never resolve here, only what was just typed (the list covers everything already saved).
    /// </summary>
    private static ConfiguredServer? ParseLiteral(string address)
    {
        string bare = (address ?? string.Empty).Trim();
        if (bare.Length == 0 || bare.Any(char.IsWhiteSpace))
            return null;

        int colon = bare.LastIndexOf(':');
        if (colon < 0)
            return new ConfiguredServer { Name = bare, Host = bare };

        if (colon == 0 || colon == bare.Length - 1
            || !ushort.TryParse(bare[(colon + 1)..], out ushort port))
            return null;

        return new ConfiguredServer
        {
            Name = bare,
            Host = bare[..colon],
            Port = port,
        };
    }
}
