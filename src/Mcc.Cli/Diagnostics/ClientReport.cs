using Mcc.Cli.Presentation;
using Mcc.Cli.Startup;
using Mcc.Cli.Configuration;
using DMCBK.Core.Configuration;
using DMCBK.Core.Localization;

namespace Mcc.Cli.Diagnostics;

/// <summary>
/// What this client is and how it was configured, as a bug report needs it.
/// <para>
/// Half of "cannot reproduce" is a setting the reporter did not think to mention: a feature gate switched off, a pinned version, a plugin nobody else runs.
/// This is that list, written once so nobody has to ask for it.
/// </para>
/// <para>
/// <b>Nothing here comes from <c>accounts.toml</c>.</b> The bundle is meant to be sent to someone else, so an account appears as its display name and kind and never as an email address, a token or a proxy credential.
/// The account NAME is kept because "it only happens on my alt" is a real report.
/// </para>
/// </summary>
internal static class ClientReport
{
    /// <summary>Collects the client report.</summary>
    /// <param name="config">The validated configuration in force.</param>
    /// <param name="console">The resolved console settings.</param>
    /// <param name="arguments">The parsed command line.</param>
    public static object Collect(DmcbkConfiguration config, ConsoleHostConfig console, CliArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(arguments);

        return new
        {
            Build = new
            {
                Version = ClientVersion.Current,
            },
            Connection = new
            {
                config.ResolvedHost,
                config.ResolvedPort,

                // Pinned vs auto matters: a pinned version that no longer matches the server is a whole class of report on its own.
                RequestedVersion = config.ResolvedVersion,
                SrvResolve = config.Connection.SrvResolve.ToString(),
                config.Connection.TcpTimeoutSeconds,
                config.Connection.Brand,
                Reconnect = new
                {
                    config.Connection.Reconnect.MaxAttempts,
                    config.Connection.Reconnect.DelaySeconds,
                    config.Connection.Reconnect.BackoffFactor,
                    config.Connection.Reconnect.RetryOnKick,
                },
            },

            // The five gates decide which subsystems exist at all, so a command that "does nothing" is very often a gate that is off.
            Gameplay = new
            {
                config.Gameplay.Terrain,
                config.Gameplay.Inventory,
                config.Gameplay.Entity,
                config.Gameplay.Physics,
                config.Gameplay.Pathfinding,
                config.Gameplay.AutoRespawn,
                config.Gameplay.MovementSpeed,
            },
            Console = new
            {
                console.ConsoleMode,
                ColorDepth = console.ColorDepth.ToString(),
                GlyphMode = console.GlyphMode.ToString(),
                console.EchoCommands,
                console.DisplayChat,
                console.Timestamps,
            },
            Logging = new
            {
                config.Logging.DebugMessages,
                config.Logging.PacketDebugMessages,
                config.Logging.LogToFile,
            },
            Localization = new
            {
                config.Localization.Language,

                // The culture the run actually resolved, which is the question a bug report needs answered: "auto" alone does not say which language the reader was seeing.
                Resolved = UiCulture.Resolve(config.Localization).Name,
            },
            CommandLine = new
            {
                // The dotted overrides are recorded by KEY only.
                // A value can carry a password when someone passes credentials that way, and this file is meant to be shared.
                OverrideKeys = arguments.Overrides.Dotted.Select(kv => kv.Key)
                    .Concat((arguments.ConsoleOverrides ?? []).Select(kv => "console." + kv.Key))
                    .ToArray(),
                arguments.Exercise,
            },
        };
    }
}
