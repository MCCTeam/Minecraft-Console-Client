using Mcc.Cli.Hosting.Classic;
using DMCBK.Core;
using DMCBK.Core.Configuration;
using DMCBK.Core.Localization;

namespace Mcc.Cli.Startup;

/// <summary>
/// The first-run login prompt: when nothing in accounts.toml can log in, ask which of the three account kinds to use and collect what that kind needs, instead of failing with "an account is required" and leaving the user to hand-write TOML.
/// <para>
/// Only what the chosen kind cannot discover for itself is asked for.
/// Offline needs a username.
/// Microsoft needs nothing: the device-code flow resolves the profile itself, and <see cref="ConsoleAuthInteraction"/> renders the code.
/// Yggdrasil needs only the provider URL, because its username and password are prompted by the auth interaction at login time; asking for them here as well would ask twice.
/// </para>
/// <para>
/// Nothing is written to disk here.
/// The caller persists the account only after a login actually succeeds (see <see cref="CliHost"/>), so a mistyped provider or an abandoned sign-in leaves the configuration untouched and the prompt runs again next start.
/// </para>
/// </summary>
internal static class GuidedLogin
{
    // Placeholder account names used as the token-cache hint for the one sign-in that resolves the real profile.
    // They never reach accounts.toml: the caller renames the account to the resolved profile before saving, and saves only on success.
    // UMPK caches a session under both the hint and the profile name, so the rename does not cost the next start its cached session.
    internal const string MicrosoftPlaceholderName = "microsoft";
    internal const string YggdrasilPlaceholderName = "yggdrasil";

    internal const int MaxOfflineNameLength = 16;

    /// <summary>
    /// True when the snapshot carries no account that could log in: no entries at all, or an active entry with neither a login nor a name for <c>ClientBuilder</c> to use as the username.
    /// </summary>
    public static bool IsNeeded(DmcbkConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.Accounts.Accounts.Count == 0
            || (string.IsNullOrWhiteSpace(config.ResolvedAccount.Login)
                && string.IsNullOrWhiteSpace(config.ResolvedAccount.Name));
    }

    /// <summary>
    /// Runs the prompt.
    /// Returns the chosen account, or null if the user answered nothing (which is also what a closed stdin looks like, so an unattended run gives up instead of looping forever).
    /// </summary>
    /// <param name="write">Writes one line to the user.</param>
    /// <param name="readLine">Reads one line, or null at end of input.</param>
    /// <param name="probe">Checks a normalized provider URL; defaults to a live HTTP probe.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<ConfiguredAccount?> PromptAsync(
        Action<string> write,
        Func<string?> readLine,
        Func<Uri, CancellationToken, Task<AuthServerProbeResult>>? probe = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(readLine);
        probe ??= AuthServerProbe.ProbeAsync;

        while (true)
        {
            write(DMCBK.Core.Localization.McStrings.mcc_auth_method_prompt);
            string? selection = readLine();
            if (string.IsNullOrWhiteSpace(selection))
                return null;

            switch (selection.Trim().ToLowerInvariant())
            {
                case "1":
                case "offline":
                    return PromptOffline(write, readLine);

                case "2":
                case "online":
                case "microsoft":
                    return new ConfiguredAccount
                    {
                        Name = MicrosoftPlaceholderName,
                        Kind = DmcbkAccountKind.MicrosoftDeviceCode,
                    };

                case "3":
                case "yggdrasil":
                    return await PromptYggdrasilAsync(write, readLine, probe, ct).ConfigureAwait(false);

                default:
                    write(DMCBK.Core.Localization.McStrings.mcc_auth_method_invalid);
                    break;
            }
        }
    }

    private static ConfiguredAccount? PromptOffline(Action<string> write, Func<string?> readLine)
    {
        while (true)
        {
            write(DMCBK.Core.Localization.McStrings.mcc_login_basic_io);
            string? name = readLine();
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // The same limit ClientBuilder.Build enforces.
            // Catching it here turns a thrown "an account is required"-style startup failure into one more line at the prompt.
            if (!TryCleanOfflineName(name, out string clean))
            {
                write(DMCBK.Core.Localization.McStrings.mcc_auth_method_offline_name_invalid);
                continue;
            }

            return new ConfiguredAccount { Name = clean, Kind = DmcbkAccountKind.Offline, Login = clean };
        }
    }

    /// <summary>
    /// Cleans a typed offline username for every host: trims, rejects empty and over-long (the same limit <see cref="ClientBuilder"/> enforces).
    /// Shared by the classic prompt above and the TUI dialog, so both refuse the same names.
    /// </summary>
    internal static bool TryCleanOfflineName(string? raw, out string clean)
    {
        clean = (raw ?? string.Empty).Trim();
        return clean.Length > 0 && clean.Length <= MaxOfflineNameLength;
    }

    private static async Task<ConfiguredAccount?> PromptYggdrasilAsync(
        Action<string> write,
        Func<string?> readLine,
        Func<Uri, CancellationToken, Task<AuthServerProbeResult>> probe,
        CancellationToken ct)
    {
        while (true)
        {
            write(DMCBK.Core.Localization.McStrings.mcc_yggdrasil_url);
            string? url = readLine();
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (!AuthServerProbe.TryNormalize(url, out Uri? provider))
            {
                write(DMCBK.Core.Localization.McStrings.mcc_yggdrasil_invalid_url);
                continue;
            }

            switch (await probe(provider, ct).ConfigureAwait(false))
            {
                case AuthServerProbeResult.Valid:
                    return new ConfiguredAccount
                    {
                        Name = YggdrasilPlaceholderName,
                        Kind = DmcbkAccountKind.Yggdrasil,
                        AuthServer = provider.AbsoluteUri,
                    };

                case AuthServerProbeResult.Unreachable:
                    write(DMCBK.Core.Localization.McStrings.mcc_yggdrasil_server_unreachable);
                    break;

                default:
                    write(DMCBK.Core.Localization.McStrings.mcc_yggdrasil_server_invalid);
                    break;
            }
        }
    }
}
