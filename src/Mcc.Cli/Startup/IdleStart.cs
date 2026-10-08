using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using DMCBK.Core;
using DMCBK.Core.Configuration;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Startup;

/// <summary>
/// Whether this run dials at startup, and what to say when it does not.
/// A client with no server, or one held back by <c>Connection.AutoConnect</c>, still builds, still loads its plugins and still runs its prompt; it simply waits for <c>connect</c> or <c>reco</c>.
/// </summary>
internal static class IdleStart
{
    /// <summary>True when the run should connect on start.</summary>
    internal static bool ShouldDial(DmcbkConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.Connection.AutoConnect && !string.IsNullOrWhiteSpace(config.ResolvedHost);
    }

    /// <summary>
    /// The line that explains why nothing is connected and what to type next, followed by the running plugins that declare <c>[services] offline</c>.
    /// Without that second line an idle client looks like a client doing nothing, when a bridge or a logger may well be working.
    /// </summary>
    internal static string Banner(DmcbkConfiguration config, Client? client, ConsoleColorDepth depth)
    {
        ArgumentNullException.ThrowIfNull(config);
        string banner = string.IsNullOrWhiteSpace(config.ResolvedHost)
            ? Strings.IdleNoServer(depth)
            : Strings.IdleAutoConnectOff($"{config.ResolvedHost}:{config.ResolvedPort}", depth);

        string[] offline = client?.PluginHost is { } host
            ? [.. host.List().Where(p => p is { Loaded: true, Offline: true })
                .Select(p => p.Id)
                .Order(StringComparer.OrdinalIgnoreCase)]
            : [];

        return offline.Length == 0
            ? banner
            : banner + "\n" + Strings.Host(Strings.IdleOfflinePlugins(string.Join(", ", offline)));
    }
}
