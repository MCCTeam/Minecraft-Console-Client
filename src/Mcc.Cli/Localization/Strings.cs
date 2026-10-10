using Mcc.Cli.Logging;
using Mcc.Cli.Plugins;
using Mcc.Cli.Presentation;
using System.Text;
using Umpk.Text;

namespace Mcc.Cli.Localization;

/// <summary>
/// The single home for the CLI host's user-facing text, so the localization seam can replace it wholesale.
/// No em dashes anywhere.
/// </summary>
internal static class Strings
{
    /// <summary>
    /// The stamp on every host notice line, so MCC's own words scan apart from typed input and server output.
    /// Matches the logger's own info prefix (<see cref="ConsoleLogger"/> emits <c>"[MCC] "</c> for Information).
    /// Applied per line by <see cref="Host"/>.
    /// </summary>
    public static string HostPrefix => TextResources.Get("cli.text.literal_58ac63c063bd");

    /// <summary>
    /// Stamps <paramref name="message"/> as MCC's own words, one <c>"[MCC] "</c> per line.
    /// Escape-safe (it only splits on newlines), so lines that already carry SGR color pass through.
    /// A same-line prompt's trailing space survives; only a fully blank line loses its trailing space.
    /// </summary>
    public static string Host(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return string.Join('\n', message
            .ReplaceLineEndings("\n")
            .Split('\n')
            .Select(line => line.Length == 0 ? HostPrefix.TrimEnd() : HostPrefix + line));
    }
    public static string Usage =>
        TextResources.Get("cli.text.literal_7639ac54989d");

    // Protocol/harness contract line.
    // Greped verbatim by the test harness; do not alter the text.
    public static string ServerJoined => TextResources.Get("cli.text.literal_8827235226ce");

    /// <summary>
    /// Printed on exit when a diagnostics bundle was written, naming the file.
    /// <para>
    /// It says the path because the whole feature is worthless if a tester cannot find the file they are being asked to send.
    /// It is the last line of the run for the same reason.
    /// </para>
    /// </summary>
    public static string DiagnosticsBundleWritten(string path)
        => TextResources.Format("cli.text.literal_504397f7b316", path);

    // Classic startup banner.
    // Mirrors the legacy ShowClassicBanner / Translations.mcc_banner_classic ("Minecraft Console Client v{0} - for MC {1} to {2} - {3}"), gated on the Display_Icon_Banner setting in console.toml. The project URL suffix is dropped: the star nudge below carries the link.
    // Light gray body, plain text when color is off; the stamp itself is added by the caller through Host and stays uncolored.
    public static string ClassicBanner(
        string clientVersion, string lowVersion, string highVersion, ConsoleColorDepth depth)
        => Ansi.Colorize(
            TextResources.Format("cli.text.literal_24d7aa0926fe", clientVersion, lowVersion, highVersion),
            TextColor.Gray, depth);

    // GitHub star nudge, printed between the classic banner and the early-build warning.
    // The star is decorative: an ASCII terminal gets the same words without it, so nobody gets a tofu box.
    public static string StarUs(bool emoji)
        => (emoji ? "⭐ " : string.Empty)
            + TextResources.Get("cli.text.literal_51b5361ef685");

    /// <summary>Shown at startup while MCC 2.0 is still being distributed only to early testers.</summary>
    public static string EarlyBuildWarningMarkdown => TextResources.Get("cli.text.literal_d38e8fa77059");

    /// <summary>
    /// Shown when the server told the client to display none of a chat message (a fully filtered mask).
    /// No content is included: the frame carries the whole signed body and the server said to withhold it.
    /// Vanilla renders nothing at all here, but a headless client that also printed nothing would leave a suppressed message indistinguishable from one that was never sent.
    /// </summary>
    public static string ChatMessageWithheld(Guid sender)
        => TextResources.Format("cli.text.literal_5370aac4cefe", sender);

    /// <summary>
    /// Shown when the server's chat stream skipped or reordered a message (1.21.5+ global chat index).
    /// Vanilla disconnects on this; UMPK reports it instead, so the host has to say so or the signal is lost.
    /// </summary>
    public static string ChatStreamGapDetected(int expected, int actual, int total)
        => TextResources.Format("cli.text.literal_c097b4180ebc", expected, actual) +
           TextResources.Format("cli.text.literal_f45ef872f71e", total);

    public static string VersionResolutionFailed(string message)
        => TextResources.Format("cli.text.literal_1211d86597cd", message);

    public static string ConnectFailed(string message)
        => TextResources.Format("cli.text.literal_f80c3c143795", message);

    public static string LoginRejected(string message)
        => TextResources.Format("cli.text.literal_af4e46e97125", message);

    public static string AuthFailed(string message)
        => TextResources.Format("cli.text.literal_cb62d73f87fc", message);

    public static string DisconnectedRemotely(string reason)
        => TextResources.Format("cli.text.literal_5f8cdaefc1c9", reason);

    // Reconnect visibility: the supervisor's Reconnecting status previously printed nothing, unlike legacy.
    // No per-attempt count is available at this event's surface, so the line stays generic.
    public static string Reconnecting => TextResources.Get("cli.text.literal_66bce4bdb48e");

    public static string Reconnected => TextResources.Get("cli.text.literal_99b922f03ddd");

    /// <summary>
    /// Renders an exception for the user as its message plus the chain of inner-exception messages.
    /// Without this the wrapping layers (for example <c>Umpk.Client.ConnectFailedException</c>, which wraps the real transport or handshake fault) print only their own generic text and the actual cause is invisible.
    /// For example, an online-mode handshake failure surfaced as nothing but "Connection failed: Could not connect to host:port."
    /// </summary>
    public static string Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var text = new StringBuilder(exception.Message);
        for (Exception? inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (string.IsNullOrWhiteSpace(inner.Message))
                continue;

            text.Append(TextResources.Get("cli.text.literal_4e69944ec126")).Append(inner.GetType().Name).Append(": ").Append(inner.Message);
        }

        return text.ToString();
    }

    // Single localized instruction for the Microsoft device-code flow.
    // The provider message repeats the URL and code, so it is not appended.
    public static string DeviceCode(string code, string url)
        => TextResources.Format("cli.text.literal_57aae658b1a7", url, code);

    // The browser flow returns its authorization code in the URL fragment, so the sign-in page displays the code instead of handing it to a local listener.
    // Say so, or the prompt below looks like it is waiting for something the user has no idea where to find.
    public static string BrowserSignIn(string url)
        => TextResources.Format("cli.text.literal_fe64251edaa2", url);

    public static string BrowserCodePrompt => TextResources.Get("cli.text.literal_81c5614c093e");

    public static string YggdrasilUsernamePrompt => TextResources.Get("cli.text.literal_89d98f48ee39");

    public static string YggdrasilPasswordPrompt => TextResources.Get("cli.text.literal_30a6a4781908");

    public static string LoginAborted => TextResources.Get("cli.text.literal_65b78f608119");

    // Server resource-pack prompt (Localization.ResourcePackPolicy = prompt).
    // Printed through the host output and answered on the input line; anything but yes/y keeps the safe default and declines.
    public static string ResourcePackQuestion(string url, bool required)
        => required
            ? TextResources.Format("cli.text.literal_b544807dcc8d", url)
            : TextResources.Format("cli.text.literal_8044e38905ec", url);

    public static string MissingFlagValue(string flag)
        => TextResources.Format("cli.text.literal_aa7a6310dda5", flag);

    public static string UnexpectedArgument(string arg)
        => TextResources.Format("cli.text.literal_fb8bdb06a970", arg);

    public static string BadFlag(string flag)
        => TextResources.Format("cli.text.literal_419a5b45a39c", flag);

    /// <summary>
    /// The friendly <c>--help</c> page, rendered as Markdown through <see cref="MarkdownConsoleRenderer"/> so headings, tables and code stand out.
    /// Plain words on purpose: it is the first thing someone who never used a terminal reads.
    /// Emojis are decorative only and are omitted when <paramref name="emoji"/> is false, so a terminal without emoji coverage gets clean headings instead of boxes.
    /// No em dashes anywhere.
    /// </summary>
    public static string HelpMarkdown(bool emoji)
    {
        string game = emoji ? "\U0001F3AE " : string.Empty;
        string wave = emoji ? "\U0001F44B " : string.Empty;
        string rocket = emoji ? "\U0001F680 " : string.Empty;
        string memo = emoji ? "\U0001F4DD " : string.Empty;
        string gear = emoji ? "⚙️ " : string.Empty;
        string key = emoji ? "\U0001F511 " : string.Empty;
        string wrench = emoji ? "\U0001F6E0️ " : string.Empty;
        string bulb = emoji ? "\U0001F4A1 " : string.Empty;
        string door = emoji ? "\U0001F6AA " : string.Empty;
        string books = emoji ? "\U0001F4DA " : string.Empty;

        return TextResources.Format("cli.text.literal_6d26bd224630", game, wave, rocket, memo, gear, key, wrench, bulb, door, books);
    }

    public static string ConfigWarning(string message)
        => Host(message);

    public static string ConfigGenerated(string folder)
        => TextResources.Format("cli.text.literal_a5e1cb9e180a", folder);

    // console.toml failed to parse: the load still falls back to defaults, but this makes the fallback visible instead of silent.
    // Wording matches DmcbkConfigurationLoader's client.toml/accounts.toml/servers.toml parse-failure message for consistency.
    public static string ConsoleConfigParseFailed(string path, string message)
        => TextResources.Format("cli.text.literal_1e20e6151ed1", path, message);

    public static string NoServerConfigured =
        TextResources.Get("cli.text.literal_d7246d846775");

    // The idle start.
    // Both lines name the command to type next, because a prompt that says only what is missing leaves the user with nothing to do about it.
    // Red lines with a yellow command, so the one notice a fresh user most needs to act on stands out; plain text when color is off.
    // The prefix itself stays uncolored so the stamp scans the same on every line.
    public static string IdleNoServer(ConsoleColorDepth depth)
        => HostPrefix
            + Ansi.Colorize(TextResources.Get("cli.text.literal_59f600b959d8"), TextColor.Red, depth)
            + Ansi.Colorize(TextResources.Get("cli.text.literal_ca74b1c38569"), TextColor.Yellow, depth)
            + Ansi.Colorize(TextResources.Get("cli.text.literal_701c26c9d9f7"), TextColor.Red, depth);

    public static string IdleAutoConnectOff(string address, ConsoleColorDepth depth)
        => HostPrefix
            + Ansi.Colorize(TextResources.Get("cli.text.literal_e75c0d0b05a4"), TextColor.Red, depth)
            + Ansi.Colorize(TextResources.Get("cli.text.literal_2816495bcf28"), TextColor.Yellow, depth)
            + Ansi.Colorize(TextResources.Format("cli.text.literal_0ade84f4935b", address), TextColor.Red, depth)
            + Ansi.Colorize(TextResources.Get("cli.text.literal_4e3b4907cdcd"), TextColor.Yellow, depth)
            + Ansi.Colorize(TextResources.Get("cli.text.literal_ddc385e3edc0"), TextColor.Red, depth);

    public static string IdleOfflinePlugins(string plugins)
        => TextResources.Format("cli.text.literal_4946a654ee0b", plugins);

    // Printed right after the guided first-run login when an online kind was picked.
    // Picking Microsoft or Yggdrasil only records the choice; the sign-in itself runs when a session starts, so without this the client looks like it swallowed the answer and dropped to an idle prompt.
    // Bright vanilla aqua (§b), the old client's info accent (old TUI McColors.Aqua 85,255,255), so the deferred promise reads as information rather than an error.
    // The prefix itself stays uncolored so the stamp scans the same on every line.
    public static string GuidedOnlineDeferred(ConsoleColorDepth depth)
        => HostPrefix + Ansi.Colorize(
            TextResources.Get("cli.text.literal_66c0345ec0ab"),
            TextColor.Aqua, depth);

    // Printed on every start that skips the guided login because an account is already configured, so a remembered account is visible instead of silently assumed.
    public static string UsingConfiguredAccount(string name)
        => TextResources.Format("cli.text.literal_b322d0389f38", name);

    // --validate-plugin <folder>, the one-shot a catalogue's CI runs per entry.
    public static string ValidatePluginUsage =
        TextResources.Format("cli.text.literal_e4657cd4751b", PluginValidateOneShot.Flag);

    public static string ValidatePluginProblem(bool error, string where, string message)
        => $"  {(error ? TextResources.Get("cli.text.literal_ca00fccfb408") : TextResources.Get("cli.text.literal_fab025c6f831"))}{(where.Length == 0 ? "  " : $" {where}  ")}{message}";

    public static string ValidatePluginOk(string id, int warnings)
        => warnings == 0
            ? TextResources.Format("cli.text.literal_0ad582b75c07", id)
            : TextResources.Format("cli.text.literal_d745ea32f80c", id, warnings);

    public static string ValidatePluginFailed(string id, int errors)
        => TextResources.Format("cli.text.literal_f823ed1195ca", id, errors);

    public static string ConfigError(string message)
        => TextResources.Format("cli.text.literal_83cb7d76bcbe", message);

    public static string LogFileError(string message)
        => TextResources.Format("cli.text.literal_c42af114cb44", message);

    public static string SendFailed(string message)
        => TextResources.Format("cli.text.literal_e4368fc759a5", message);

    // Plugin host bootstrap.
    public static string PluginsLoaded(string summary) => Host(summary);

    public static string PluginsLoadError(string message) => Host(TextResources.Format("cli.text.literal_4e1cb9d75815", message));

    // The install confirmation.
    // The market renders the description; these two lines are the host's part, because only a host knows whether anybody can answer.
    public static string PluginInstallQuestion => TextResources.Get("cli.text.literal_d371e0e23d27");

    public static string PluginInstallNotInteractive =>
        TextResources.Get("cli.text.literal_a074a149bd17");

    public static string MarketAutoUpdateFailed => TextResources.Get("cli.text.literal_ae197a8f92b5");

    // Host command text (clear-console/console-chat/exit).
    public static string ClearConsoleDesc => TextResources.Get("cli.text.literal_2ccca9fea8d6");

    // Written after the wipe.
    // Not decoration: it is the write that forces ConsoleBuffer.RedrawInputArea(RedrawAll: true) so the "> " prompt comes back.
    // See RichConsole.ClearScreen.
    public static string ConsoleChatDesc => TextResources.Get("cli.text.literal_fd45b4af4698");
    public static string ConsoleChatOn => TextResources.Get("cli.text.literal_bdaace8d8c1e");
    public static string ConsoleChatOff => TextResources.Get("cli.text.literal_43c6313dc39b");
    public static string ExitDesc => TextResources.Get("cli.text.literal_4fb602e50744");

    // --exercise <name> scripted diagnostic.
    // The PASS/FAIL/SKIP lines are the machine-greppable contract; the check tokens (for example "world.time") are technical identifiers and stay verbatim.
    public static string UnknownExercise(string name)
        => TextResources.Format("cli.text.literal_90db4e4d2862", name);

    public static string ExerciseStart(string name)
        => TextResources.Format("cli.text.literal_981476a511f2", name);

    public static string ExerciseSummary(string name, int passed, int failed, int skipped)
        => TextResources.Format("cli.text.literal_c8fa8a64d9c8", name, passed, failed, skipped);

    public static string ExercisePass(string check, string detail)
        => detail.Length == 0 ? TextResources.Format("cli.text.literal_b57a5b51368c", check) : TextResources.Format("cli.text.literal_cf7377489912", check, detail);

    public static string ExerciseFail(string check, string detail)
        => TextResources.Format("cli.text.literal_20fa24bbaee4", check, detail);

    public static string ExerciseSkip(string check, string reason)
        => TextResources.Format("cli.text.literal_d968213dc0e3", check, reason);

    // TUI backend.
    // All TUI user-facing text lives here; no hardcoded strings in the views.
    public static string TuiBannerTitle => TextResources.Get("cli.text.literal_28900be5201e");
    public static string TuiBannerSubtitle => TextResources.Get("cli.text.literal_6385098592f9");
    public static string TuiBannerVersionsLabel => TextResources.Get("cli.text.literal_bbfeea01bf02");
    public static string TuiBannerHint => TextResources.Get("cli.text.literal_cc28b0f9edc9");
    public static string TuiInputWatermark => TextResources.Get("cli.text.literal_ef30ee1d9535");
    public static string TuiQuitHint => TextResources.Get("cli.text.literal_1e9ff4643d45");
    public static string TuiStatusOffline => TextResources.Get("cli.text.literal_98b1024757db");

    // TUI welcome, written to the TUI log on startup instead of the classic banner/star/warning that the plain console gets.
    // Same content, native controls: the TUI paints its own cells and would show the classic host's SGR escapes and Markdown panel as literal characters.
    public static string TuiWelcomeEarlyBuild => TextResources.Get("cli.text.literal_4a53f75d0c97");
    public static string TuiWelcomeUnfinished =>
        TextResources.Get("cli.text.literal_4f226465d464");

    // TUI local dialogs (first-run login, auth).
    // Buttons answer, Esc cancels, Enter presses the first button.
    // All TUI user-facing text lives here; no hardcoded strings in the views.
    public static string TuiLoginTitle => TextResources.Get("cli.text.literal_8858646c693d");
    public static string TuiLoginOffline => TextResources.Get("cli.text.literal_a1794783aab7");
    public static string TuiLoginOnline => TextResources.Get("cli.text.literal_ec5c40e4d832");
    public static string TuiLoginYggdrasil => TextResources.Get("cli.text.literal_e7b53c341932");
    public static string TuiLoginUsernameTitle => TextResources.Get("cli.text.literal_e77b86276405");
    public static string TuiLoginYggdrasilTitle => TextResources.Get("cli.text.literal_343aead3417e");
    public static string TuiFieldUsername => TextResources.Get("cli.text.literal_e3b89e9d33f8");
    public static string TuiFieldPassword => TextResources.Get("cli.text.literal_e7cf3ef4f17c");
    public static string TuiFieldCode => TextResources.Get("cli.text.literal_340f463033e0");
    public static string TuiFieldProviderUrl => TextResources.Get("cli.text.literal_c3e446ef62b7");
    public static string TuiPromptContinue => TextResources.Get("cli.text.literal_31fbef162594");
    public static string TuiPromptBack => TextResources.Get("cli.text.literal_76900f1bfd16");
    public static string TuiPromptClose => TextResources.Get("cli.text.literal_7d9eb7acb13e");
    public static string TuiPromptCancel => TextResources.Get("cli.text.literal_19766ed6ccb2");
    public static string TuiPromptOk => TextResources.Get("cli.text.literal_565339bc4d33");
    public static string TuiPromptSignIn => TextResources.Get("cli.text.literal_bfd402b2f6f3");
    public static string TuiPromptOpenBrowser => TextResources.Get("cli.text.literal_62a2dc1bd597");
    public static string TuiPromptCopyCode => TextResources.Get("cli.text.literal_49a0053f3b0d");
    public static string TuiAuthDeviceTitle => TextResources.Get("cli.text.literal_92fa49e98832");
    public static string TuiAuthDeviceBody => TextResources.Get("cli.text.literal_18f74b565455");
    public static string TuiAuthBrowserFailed => TextResources.Get("cli.text.literal_90587ae5a23f");
    public static string TuiAuthCopied => TextResources.Get("cli.text.literal_af09632242f3");
    public static string TuiAuthCopyFailed => TextResources.Get("cli.text.literal_64528d336a1d");
    public static string TuiAuthFailedHeading => TextResources.Get("cli.text.literal_3c0c7d587f15");
    public static string TuiLoginSignedIn(string profile)
        => TextResources.Format("cli.text.literal_914923a34ac0", profile);
    public static string TuiAuthBrowserTitle => TextResources.Get("cli.text.literal_92fa49e98832");
    public static string TuiAuthYggdrasilTitle => TextResources.Get("cli.text.literal_afbf8a3a4b55");
    // TUI plugin manager (the visual /plugins ui).
    // All manager text lives here; no hardcoded strings in the views.
    // Wording mirrors the text verbs where it reports the same thing.
    public static string PmTitle => TextResources.Get("cli.text.literal_9514b7ff4860");
    public static string PmManagerKicker => TextResources.Get("cli.text.literal_27821e54e753");
    public static string PmManagerSubtitle => TextResources.Get("cli.text.literal_7473b20bd659");
    public static string PmClose => TextResources.Get("cli.text.literal_7d9eb7acb13e");
    public static string PmLibrary => TextResources.Get("cli.text.literal_d6ee4272fa92");
    public static string PmLibrarySummary(int discovered, int enabled, int loaded)
        => TextResources.Format("cli.text.literal_ffbdc682dff5", discovered, enabled, loaded);
    public static string PmDiscover => TextResources.Get("cli.text.literal_d4a33d5b78bc");
    public static string PmMaintenance => TextResources.Get("cli.text.literal_17ccfa5b681e");
    public static string PmOverview => TextResources.Get("cli.text.literal_d4b1ea5708dd");
    public static string PmActions => TextResources.Get("cli.text.literal_ff8059dc6752");
    public static string PmPluginDetailSubtitle => TextResources.Get("cli.text.literal_3e5e51281010");
    public static string PmSettingsSubtitle => TextResources.Get("cli.text.literal_54b8e7314b9c");
    public static string PmInstallSubtitle => TextResources.Get("cli.text.literal_2b91701b6f31");
    public static string PmUpdatesSubtitle => TextResources.Get("cli.text.literal_431a072dfae4");
    public static string PmMarketsSubtitle => TextResources.Get("cli.text.literal_a35e954d941d");
    public static string PmSearchSubtitle => TextResources.Get("cli.text.literal_9b3cce951d61");
    public static string PmReportsSubtitle => TextResources.Get("cli.text.literal_d21c7921a7b4");
    public static string PmFilterAll => TextResources.Get("cli.text.literal_a52ace420f21");
    public static string PmFilterOutdated => TextResources.Get("cli.text.literal_c759f42e4568");
    public static string PmFilterLocal => TextResources.Get("cli.text.literal_8c31e6e72230");
    public static string PmFilterEnabled => TextResources.Get("cli.text.literal_92c1cdfdf4cb");
    public static string PmFilterDisabled => TextResources.Get("cli.text.literal_75081b593d15");
    public static string PmFilterCount(string filter, int count) => $"{filter} ({count})";
    public static string PmEntryDll => TextResources.Get("cli.text.literal_0343b13708cd");
    public static string PmEntrySource => TextResources.Get("cli.text.literal_41cf6794ba42");
    public static string PmOfflineTag => TextResources.Get("cli.text.literal_89aa6bd39b61");
    public static string PmOutdatedTag(string version) => TextResources.Format("cli.text.literal_263ae31c6de8", version);
    public static string PmNotLoadedTag(string status) => TextResources.Format("cli.text.literal_358db8f28af8", status);
    public static string PmNoneDiscovered => TextResources.Get("cli.text.literal_1193005329ba");
    public static string PmListEmpty(string filter) => TextResources.Format("cli.text.literal_3e0c5140a0a8", filter);
    public static string PmNoMarket => TextResources.Get("cli.text.literal_db4e38dc7a83");
    public static string PmNoHost => TextResources.Get("cli.text.literal_71bd00226766");
    public static string PmListHints => TextResources.Get("cli.text.literal_e8e53908445d");
    public static string PmInstall => TextResources.Get("cli.text.literal_569ca49f4aaf");
    public static string PmMarketplaces => TextResources.Get("cli.text.literal_dfe262442ec7");
    public static string PmUpdates => TextResources.Get("cli.text.literal_22e2bada8f1c");
    public static string PmUpdateAll => TextResources.Get("cli.text.literal_95e50255e771");
    public static string PmReloadAll => TextResources.Get("cli.text.literal_f4a8c472746d");
    public static string PmDoctor => TextResources.Get("cli.text.literal_24669ff48290");
    public static string PmNew => TextResources.Get("cli.text.literal_18fdd549b2ed");
    public static string PmValidate => TextResources.Get("cli.text.literal_40611abbdda2");
    public static string PmLoad => TextResources.Get("cli.text.literal_8a6bdb6b18da");
    public static string PmEnable => TextResources.Get("cli.text.literal_5342e09f2729");
    public static string PmDisable => TextResources.Get("cli.text.literal_b7e3e4aa4257");
    public static string PmReload => TextResources.Get("cli.text.literal_bdc090ec61e3");
    public static string PmUnload => TextResources.Get("cli.text.literal_6b88fe530b0a");
    public static string PmUpdate => TextResources.Get("cli.text.literal_c1c1009d3f37");
    public static string PmPin => TextResources.Get("cli.text.literal_ff1cee744146");
    public static string PmUnpin => TextResources.Get("cli.text.literal_ee3c71613054");
    public static string PmSettings => TextResources.Get("cli.text.literal_74a883a037bc");
    public static string PmUninstall => TextResources.Get("cli.text.literal_fe199528850a");
    public static string PmSave => TextResources.Get("cli.text.literal_1509f561f241");
    public static string PmResetDefaults => TextResources.Get("cli.text.literal_e240e635ff6d");
    public static string PmRegenComments => TextResources.Get("cli.text.literal_9247a396bd27");
    public static string PmBack => TextResources.Get("cli.text.literal_76900f1bfd16");
    public static string PmRefresh => TextResources.Get("cli.text.literal_0e9161011702");
    public static string PmRefreshAll => TextResources.Get("cli.text.literal_3c128b5436b6");
    public static string PmRemove => TextResources.Get("cli.text.literal_c3812fc4acb8");
    public static string PmAddMarketplace => TextResources.Get("cli.text.literal_8b89f8059ef4");
    public static string PmSearch => TextResources.Get("cli.text.literal_49c266baaaa7");
    public static string PmShowInstalled => TextResources.Get("cli.text.literal_b89c37f11d7e");
    public static string PmScaffold => TextResources.Get("cli.text.literal_2c30f266c726");
    public static string PmSetPin => TextResources.Get("cli.text.literal_c3fc3d49cff9");
    public static string PmDetailHints => TextResources.Get("cli.text.literal_6c575c6335d3");
    public static string PmUninstallTitleFor(string id) => TextResources.Format("cli.text.literal_9345808c614e", id);
    public static string PmPurgeLabel => TextResources.Get("cli.text.literal_a29e86447750");
    public static string PmPurgeNote(string id) => TextResources.Format("cli.text.literal_fd52ea6c7220", id);
    public static string PmStateEnabledLoaded => TextResources.Get("cli.text.literal_6f3f5c181e0a");
    public static string PmStateEnabledNotLoaded => TextResources.Get("cli.text.literal_d826c8f48693");
    public static string PmStateDisabled => TextResources.Get("cli.text.literal_17eb3c0168d0");
    public static string PmInfoNotLoaded => TextResources.Get("cli.text.literal_df5c0f506268");
    public static string PmInfoEntry => TextResources.Get("cli.text.literal_923fe53966c6");
    public static string PmInfoFolder => TextResources.Get("cli.text.literal_034a00624882");
    public static string PmInfoSource => TextResources.Get("cli.text.literal_41cf6794ba42");
    public static string PmInfoMarket => TextResources.Get("cli.text.literal_e4d693d24e64");
    public static string PmInfoInstalled => TextResources.Get("cli.text.literal_a51a6c19a1ff");
    public static string PmInfoPinned => TextResources.Get("cli.text.literal_3fab5c181bd2");
    public static string PmInfoPinnedAtVersion(string version) => TextResources.Format("cli.text.literal_2afa9dcec49d", version);
    public static string PmInfoAbout => TextResources.Get("cli.text.literal_a4262e1c9bcb");
    public static string PmInfoHomepage => TextResources.Get("cli.text.literal_a5c5a15eec44");
    public static string PmInfoTags => TextResources.Get("cli.text.literal_978c2f894135");
    public static string PmInfoUses => TextResources.Get("cli.text.literal_f480ffb2979d");
    public static string PmInfoRequires => TextResources.Get("cli.text.literal_b9014eaab626");
    public static string PmInfoOptional => TextResources.Get("cli.text.literal_ec91fdd9256c");
    public static string PmInfoNeededBy => TextResources.Get("cli.text.literal_a9cc0583b2ae");
    public static string PmInfoExports => TextResources.Get("cli.text.literal_4b4e49dd2de5");
    public static string PmInfoLanguages => TextResources.Get("cli.text.literal_6cb574fa10b6");
    public static string PmInfoManual => TextResources.Get("cli.text.literal_36bde66f289a");
    public static string PmInfoOffline => TextResources.Get("cli.text.literal_8e2c7ac50813");
    public static string PmInfoOfflineYes => TextResources.Get("cli.text.literal_139c93a1894d");
    public static string PmInfoErrors => TextResources.Get("cli.text.literal_be4bd5677277");
    public static string PmInfoLocal => TextResources.Get("cli.text.literal_115676b586cc");
    public static string PmSettingsTitle(string id) => TextResources.Format("cli.text.literal_fed9f52dcd92", id);
    public static string PmNoSettingsFile => TextResources.Get("cli.text.literal_793cad45c795");
    public static string PmSettingsSavedNothing => TextResources.Get("cli.text.literal_a999ab924579");
    public static string PmSettingsHints => TextResources.Get("cli.text.literal_c238a3a25e64");
    public static string PmResetTitle => TextResources.Get("cli.text.literal_9d6440a4d5c8");
    public static string PmResetTitleFor(string id) => TextResources.Format("cli.text.literal_21fec9c32445", id);
    public static string PmResetBody => TextResources.Get("cli.text.literal_0b52fe31b4fd");
    public static string PmReset => TextResources.Get("cli.text.literal_daee7606b339");
    public static string PmInstallTitle => TextResources.Get("cli.text.literal_2364766974ef");
    public static string PmInstallChoiceHint => TextResources.Get("cli.text.literal_b43126151f1b");
    public static string PmInstallById => TextResources.Get("cli.text.literal_b964ecd32465");
    public static string PmBrowsePlugins => TextResources.Get("cli.text.literal_31786be3db06");
    public static string PmFieldPluginId => TextResources.Get("cli.text.literal_32a723fa23c1");
    public static string PmFieldSource => TextResources.Get("cli.text.literal_0e570ca6fabe");
    public static string PmFieldVersion => TextResources.Get("cli.text.literal_dd167905de0d");
    public static string PmInstallHint => TextResources.Get("cli.text.literal_e6e8f04c2b08");
    public static string PmInstallNeedsSource => TextResources.Get("cli.text.literal_e649494b74f8");
    public static string PmAvailablePluginsTitle => TextResources.Get("cli.text.literal_f5544c46132d");
    public static string PmAvailablePluginsSubtitle => TextResources.Get("cli.text.literal_268b3ddc3773");
    public static string PmMarketplacePluginsTitle(string marketplace) => TextResources.Format("cli.text.literal_3ae3ab393e8f", marketplace);
    public static string PmMarketplacePluginsSubtitle(string marketplace) => TextResources.Format("cli.text.literal_ee4026a4e450", marketplace);
    public static string PmAvailablePluginsHeader(int count) => TextResources.Format("cli.text.literal_7205994f06cd", count);
    public static string PmNoAvailablePlugins => TextResources.Get("cli.text.literal_4bead9e5e32d");
    public static string PmNoFilteredPlugins => TextResources.Get("cli.text.literal_e1dedc12ba51");
    public static string PmNoMarketplacePlugins(string marketplace) => TextResources.Format("cli.text.literal_0da51150b9ef", marketplace);
    public static string PmAvailablePluginsHints => TextResources.Get("cli.text.literal_1f7471a28717");
    public static string PmConfirmInstallTitle => TextResources.Get("cli.text.literal_569ca49f4aaf");
    public static string PmConfirmInstallTitleFor(string id) => TextResources.Format("cli.text.literal_2cfbc93920cd", id);
    public static string PmUpdatesTitle => TextResources.Get("cli.text.literal_22e2bada8f1c");
    public static string PmUpdatesHeader(int available, int held) => TextResources.Format("cli.text.literal_86263ca0365c", available, held);
    public static string PmNoUpdates => TextResources.Get("cli.text.literal_bab60aea2a05");
    public static string PmBlockedTagFor(string reason) => TextResources.Format("cli.text.literal_20633c969e94", reason);
    public static string PmPinnedTag => TextResources.Get("cli.text.literal_6f7301c0cbf8");
    public static string PmPinTitle => TextResources.Get("cli.text.literal_ff1cee744146");
    public static string PmPinTitleFor(string id) => TextResources.Format("cli.text.literal_67fd141b49bc", id);
    public static string PmFieldPinVersion => TextResources.Get("cli.text.literal_6a6939604d95");
    public static string PmUpdatesHints => TextResources.Get("cli.text.literal_a2013da8a768");
    public static string PmMarketsTitle => TextResources.Get("cli.text.literal_dfe262442ec7");
    public static string PmMarketsHeader(int count) => TextResources.Format("cli.text.literal_861ff60d86e3", count);
    public static string PmNoMarkets => TextResources.Get("cli.text.literal_5c820c59051e");
    public static string PmPolicyLabel => TextResources.Get("cli.text.literal_c611981fab98");
    public static string PmPluginsColumn => TextResources.Get("cli.text.literal_9514b7ff4860");
    public static string PmRefreshedColumn => TextResources.Get("cli.text.literal_62b59354741e");
    public static string PmNeverRefreshed => TextResources.Get("cli.text.literal_04ad10be4d22");
    public static string PmRefreshedAt(string when) => TextResources.Format("cli.text.literal_6521f377d71f", when);
    public static string PmMarketsHints => TextResources.Get("cli.text.literal_b4e3d866b94f");
    public static string PmRemoveMarketTitle => TextResources.Get("cli.text.literal_0f8b8c44303c");
    public static string PmRemoveMarketTitleFor(string name) => TextResources.Format("cli.text.literal_9d64ce84e2c3", name);
    public static string PmRemoveMarketBody => TextResources.Get("cli.text.literal_f3176c4bb18f");
    public static string PmAddMarketTitle => TextResources.Get("cli.text.literal_8b89f8059ef4");
    public static string PmFieldMarketSource => TextResources.Get("cli.text.literal_0e570ca6fabe");
    public static string PmFieldMarketName => TextResources.Get("cli.text.literal_dcd1d5223f73");
    public static string PmAddMarketHint => TextResources.Get("cli.text.literal_f682fbfa11cd");
    public static string PmAddMarketNeedsSource => TextResources.Get("cli.text.literal_668b11a3802c");
    public static string PmSearchTitle => TextResources.Get("cli.text.literal_df08b7498d9a");
    public static string PmFieldQuery => TextResources.Get("cli.text.literal_822b2ae4542b");
    public static string PmFieldMarketplace => TextResources.Get("cli.text.literal_582967534d0f");
    public static string PmMarketplaceAll => TextResources.Get("cli.text.literal_d3789239dbcd");
    public static string PmSearchNeedsQuery => TextResources.Get("cli.text.literal_87ee3dd0bfd9");
    public static string PmSearchStaleHint => TextResources.Get("cli.text.literal_d86543290ca8");
    public static string PmSearchResultsHints => TextResources.Get("cli.text.literal_06c28640fd5b");
    public static string PmSearchInstalled(string version) => TextResources.Format("cli.text.literal_ed6ef39d1654", version);
    public static string PmSearchNone(string text) => TextResources.Format("cli.text.literal_5326d7d8cd0b", text);
    public static string PmDoctorTitle => TextResources.Get("cli.text.literal_24669ff48290");
    public static string PmDoctorHeader(int discovered, int loaded)
        => TextResources.Format("cli.text.literal_5f8242f5669e", discovered, loaded);
    public static string PmDoctorHealthy => TextResources.Get("cli.text.literal_1d5e8f8b9206");
    public static string PmValidateTitle => TextResources.Get("cli.text.literal_40611abbdda2");
    public static string PmValidateHeader(int count, int bad)
        => bad == 0
            ? TextResources.Format("cli.text.literal_1de62af373e4", count)
            : TextResources.Format("cli.text.literal_8d752d83f14d", count, bad);
    public static string PmValidateSubjectOk(string id) => TextResources.Format("cli.text.literal_ca09727312a7", id);
    public static string PmNewTitle => TextResources.Get("cli.text.literal_75efa2eee091");
    public static string PmFieldId => TextResources.Get("cli.text.literal_12156e921bdb");
    public static string PmNewHint => TextResources.Get("cli.text.literal_564aa182db5e");
    public static string PmNewNeedsId => TextResources.Get("cli.text.literal_44ddb93f39ae");
    public static string PmLoadTitle => TextResources.Get("cli.text.literal_1f4e1943ca5c");
    public static string PmFieldPath => TextResources.Get("cli.text.literal_62fa5a5b0d3c");
    public static string PmLoadHint => TextResources.Get("cli.text.literal_494f35043626");
    public static string PmLoadNeedsPath => TextResources.Get("cli.text.literal_2f94d3cd3dc7");
    public static string PmSeverityError => TextResources.Get("cli.text.literal_ca00fccfb408");
    public static string PmSeverityWarning => TextResources.Get("cli.text.literal_fab025c6f831");
    public static string PmSeverityNote => TextResources.Get("cli.text.literal_414627b6a45d");
    public static string PmWorking => TextResources.Get("cli.text.literal_5474eef8d0f1");

    // TUI connect dialog (idle startup): the saved servers as buttons, or a new-server form.
    public static string TuiConnectTitle => TextResources.Get("cli.text.literal_de6cce08571e");
    public static string TuiConnectNewServer => TextResources.Get("cli.text.literal_6b8ab90b76e5");
    public static string TuiConnectNewTitle => TextResources.Get("cli.text.literal_c2ee50f7d4b2");
    public static string TuiFieldHost => TextResources.Get("cli.text.literal_4a823118b9ba");
    public static string TuiFieldPort => TextResources.Get("cli.text.literal_9260a2229e12");
    public static string TuiFieldServerName => TextResources.Get("cli.text.literal_5a60e60d1fd2");
    public static string TuiConnectConnect => TextResources.Get("cli.text.literal_1a2303ede074");
    public static string TuiConnectSaveConnect => TextResources.Get("cli.text.literal_3466c1c0c085");
    public static string TuiConnectExitClient => TextResources.Get("cli.text.literal_1bc4db23e417");
    public static string TuiConnectNeedsHost => TextResources.Get("cli.text.literal_3beb074204a9");
    public static string TuiConnectBadAddress => TextResources.Get("cli.text.literal_ef64534c449c");
    public static string TuiConnectNeedsName => TextResources.Get("cli.text.literal_166e7a880257");
    public static string TuiConnectNoConfig => TextResources.Get("cli.text.literal_20a5e12b0cb6");
    public static string TuiConnectSaved(string name, string host, int port)
        => TextResources.Format("cli.text.literal_9df3adfa5292", name, host, port);
    public static string TuiConnectSaveFailed(string reason)
        => TextResources.Format("cli.text.literal_fcef7f185767", reason);

    // Server-status/MOTD panel, shown on every connect attempt in both the classic and TUI hosts.
    // Wording mirrors legacy's mcc.server_info.* keys (MinecraftClient/Resources/Translations/Translations.resx).
    public static string ServerInfoLabelServer => TextResources.Get("cli.text.literal_087fbe073393");
    public static string ServerInfoLabelVersion => TextResources.Get("cli.text.literal_4c5726ee16ee");
    public static string ServerInfoLabelProtocol(int protocol) => TextResources.Format("cli.text.literal_a3938a08cc29", protocol);
    public static string ServerInfoProtocolUnknown => TextResources.Get("cli.text.literal_7b34d7b5feb1");
    public static string ServerInfoVersionUnknown => "?";
    public static string ServerInfoLabelConnectingAs => TextResources.Get("cli.text.literal_2abe6b361115");
    public static string ServerInfoLabelPing => TextResources.Get("cli.text.literal_3175cc0386c2");
    public static string ServerInfoPingMs(long ms) => TextResources.Format("cli.text.literal_978a6092f1e3", ms);
    public static string ServerInfoLabelPlayers => TextResources.Get("cli.text.literal_981bc21a0a9b");
    public static string ServerInfoLabelOnlinePlayers => TextResources.Get("cli.text.literal_d291bf6d1c5b");
    public static string ServerInfoSampleMore(int count) => $"... +{count}";

    // TUI container / book / dialog / tab / minimap views.
    // Q drops one, Ctrl+Q drops the whole hovered stack (both are wired to InventoryApi.DropAsync).
    public static string TuiContainerClose => TextResources.Get("cli.text.literal_7d9eb7acb13e");
    public static string TuiContainerControls => TextResources.Get("cli.text.literal_beb0272f6cc3");
    public static string TuiContainerEmpty => TextResources.Get("cli.text.literal_d29a0939bcb5");
    public static string TuiCursorLabel => TextResources.Get("cli.text.literal_fd528823e8d3");
    public static string TuiBookTitleLabel => TextResources.Get("cli.text.literal_ecd11fd9f6ba");
    public static string TuiBookControls => TextResources.Get("cli.text.literal_ae50f39ae8ae");
    public static string TuiBookSigned => TextResources.Get("cli.text.literal_c30c7308a726");
    public static string TuiBookSaved => TextResources.Get("cli.text.literal_6dfc960eea70");
    public static string TuiBookSignPrompt => TextResources.Get("cli.text.literal_f4054366eac0");
    public static string TuiBookEditUnsupported =>
        TextResources.Get("cli.text.literal_eb61adafded9");

    // Book protocol-limit validation, checked before every save/sign against DMCBK.Core's BookLimits.
    public static string TuiBookTooManyPages(int count, int max)
        => TextResources.Format("cli.text.literal_34731889fd4a", count, max);

    public static string TuiBookPageTooLong(int page, int length, int max)
        => TextResources.Format("cli.text.literal_8d0b457c6907", page, length, max);

    public static string TuiBookTitleTooLong(int max) => TextResources.Format("cli.text.literal_e4fb7d364272", max);

    public static string TuiBookMaxPagesReached(int max) => TextResources.Format("cli.text.literal_77632ee14cf1", max);

    // Escape-with-unsaved-edits confirmation: shown once, a second Escape discards.
    public static string TuiBookUnsavedConfirm => TextResources.Get("cli.text.literal_2314b6665840");
    public static string TuiDialogControls => TextResources.Get("cli.text.literal_676b33dbcd67");
    public static string TuiDialogInputs => TextResources.Get("cli.text.literal_7abc49dfa87b");
    public static string TuiDialogNoButtons => TextResources.Get("cli.text.literal_5e583f0b7f95");
    public static string TuiDialogCancel => TextResources.Get("cli.text.literal_87038c5aa35d");
    public static string TuiDialogSent => TextResources.Get("cli.text.literal_cb6f44b8e237");
    public static string TuiDialogClosedOnly => TextResources.Get("cli.text.literal_2d700107fb15");
    public static string TuiDialogNotPerformed =>
        TextResources.Get("cli.text.literal_41d780ed369b");

    public static string TuiDialogCommandRun(string command) => TextResources.Format("cli.text.literal_e5488e0793cc", command);
    public static string TuiTabControls => TextResources.Get("cli.text.literal_0fdf605ea721");
    /// <summary>The "showing x-y of N" hint under an overflowing suggestion popup.</summary>
    public static string TuiSuggestionRange(int from, int to, int total) => $"[{from}-{to}/{total}]";

    /// <summary>Title of the TUI view of the player's own window; matches the legacy window title.</summary>
    /// <summary>
    /// Announced when the server transfers the session elsewhere.
    /// Legacy logged this with a bare interpolated string (McClient.cs:426) rather than a Translations key, so there is no corpus entry to reuse and it lives here with the other host-only text.
    /// </summary>
    public static string TransferInitiated(string host, int port) => TextResources.Format("cli.text.literal_b38c62c3420f", host, port);

    public static string TuiPlayerInventoryTitle => TextResources.Get("cli.text.literal_76e5b8337dfd");

    // /map fullscreen overlay.
    public static string MapDesc => TextResources.Get("cli.text.literal_cd0033cb5f84");
    public static string TuiMapNoData => TextResources.Get("cli.text.literal_f7b4bc2d1872");

    public static string TuiMapOpening(int mapId) => TextResources.Format("cli.text.literal_3d2ef2ed56ad", mapId);

    public static string TuiMapFooter(int mapId, byte scale, string zoomPercent)
        => TextResources.Format("cli.text.literal_503e99a7f80a", mapId, scale, zoomPercent);

    public static string TuiBookPageHeader(int page, int total) => TextResources.Format("cli.text.literal_88c03de25a8b", page, total);

    public static string TuiDialogRegistryOnly(int registryId)
        => TextResources.Format("cli.text.literal_0ef3e11d6ec1", registryId);

    public static string TuiDialogInputLabel(string key, string label, string kind)
        => label.Length == 0 ? $"{key} ({kind})" : $"{label} [{key}, {kind}]";

    public static string TuiDialogButton(int index, string label) => $"[{index}] {label}";

    public static string TuiTabHeader(int count) => TextResources.Format("cli.text.literal_947addf1de01", count);

    // Tab-list table: column headings and the empty-list message.
    public static string TuiTabNoPlayers => TextResources.Get("cli.text.literal_80fb7ca33083");
    public static string TuiTabColumnPing => TextResources.Get("cli.text.literal_6a4c3a4483b0");
    public static string TuiTabColumnTeam => TextResources.Get("cli.text.literal_5985039f106d");
    public static string TuiTabColumnPlayer => TextResources.Get("cli.text.literal_64aee8c6cbd0");

    // Minimap richness: compass + legend, ported from legacy's info bar/legend row.
    // Replaces the old placeholder "XYZ x y z zN" info line with the compass-arrow format below (no other call site).
    public static string MinimapWindowTitle => TextResources.Get("cli.text.literal_4bad6cff07d2");
    public static string TuiMainMenuButton => TextResources.Get("cli.text.literal_276c1c4f657d");
    public static string TuiExperienceValue(int level, int total) => TextResources.Format("cli.text.literal_0429a401109a", level, total);
    public static string MinimapInfoCompass(int x, int y, int z, string arrow, int zoom, bool cave)
        => cave ? $"{x}, {y}, {z}  {arrow}  {zoom}:1  ▼" : $"{x}, {y}, {z}  {arrow}  {zoom}:1";

    public static string MinimapLegendHostile => TextResources.Get("cli.text.literal_8f383ccddc6f");
    public static string MinimapLegendPassive => TextResources.Get("cli.text.literal_a7f1d7bc2124");
    public static string MinimapLegendNeutral => TextResources.Get("cli.text.literal_7e2372f4115c");
    public static string MinimapLegendPlayer => TextResources.Get("cli.text.literal_cdb59355f3ba");

    public static string TuiContainerTitle(string title, int windowId) => TextResources.Format("cli.text.literal_3f41e4fd31c2", title, windowId);

    // Chat signature-standing markers.
    // The bar is the ASCII stand-in for the legacy coloured block and is only meaningful with colour; the word tags are what a colourless terminal shows instead, and only for the standings a reader needs to act on.
    public static string ChatStandingBar => "|";
    public static string ChatStandingRejected => TextResources.Get("cli.text.literal_82a7300f878b");
    public static string ChatStandingUnverified => TextResources.Get("cli.text.literal_7a9536ae1ecd");
    public static string ChatStandingInsecure => TextResources.Get("cli.text.literal_9a5fe6fd8de5");

    // Colour-free standing names, for logs and diagnostics.
    public static string ChatStandingLabelVerified => TextResources.Get("cli.text.literal_1c34f88707b5");
    public static string ChatStandingLabelUnverified => TextResources.Get("cli.text.literal_97b7e2db799e");
    public static string ChatStandingLabelRejected => TextResources.Get("cli.text.literal_7fc1756bbb9b");
    public static string ChatStandingLabelInsecure => TextResources.Get("cli.text.literal_1d92dae504a7");
    public static string ChatStandingLabelNotApplicable => TextResources.Get("cli.text.literal_976d6318e648");

    // Shared management-browser workspace.
    public static string MgmtBack => TextResources.Get("cli.text.literal_76900f1bfd16");
    public static string MgmtClose => TextResources.Get("cli.text.literal_7d9eb7acb13e");
    public static string MgmtSearch => TextResources.Get("cli.text.literal_49c266baaaa7");
    public static string MgmtRefresh => TextResources.Get("cli.text.literal_0e9161011702");
    public static string MgmtHints => TextResources.Get("cli.text.literal_d751bbf6cec5");
    public static string MgmtNoMatches => TextResources.Get("cli.text.literal_c0424120b36f");
    public static string MgmtShowing(int shown, int total) => TextResources.Format("cli.text.literal_ef2af81f497d", shown, total);

    // Main management menu.
    public static string MccMenuTitle => TextResources.Get("cli.text.literal_e5acb7f00e4f");
    public static string MccMenuSubtitle => TextResources.Get("cli.text.literal_658a3cd75401");
    public static string MccMenuLearn => TextResources.Get("cli.text.literal_b210586c2ae1");
    public static string MccMenuLive => TextResources.Get("cli.text.literal_c8b4b901d631");
    public static string MccMenuExtend => TextResources.Get("cli.text.literal_a7094b8d7523");
    public static string MccMenuClient => TextResources.Get("cli.text.literal_50d7026e31bc");
    public static string MccMenuCommands => TextResources.Get("cli.text.literal_080f6943b359");
    public static string MccMenuCommandsDescription => TextResources.Get("cli.text.literal_6029fe008077");
    public static string MccMenuCommandsHint => TextResources.Get("cli.text.literal_a0881a435b56");
    public static string MccMenuScripts => TextResources.Get("cli.text.literal_8d0e6b4ab605");
    public static string MccMenuScriptsDescription => TextResources.Get("cli.text.literal_4cae8073b25c");
    public static string MccMenuScriptsHint => TextResources.Get("cli.text.literal_f767c0971b20");
    public static string MccMenuInventory => TextResources.Get("cli.text.literal_1610025343ae");
    public static string MccMenuInventoryDescription => TextResources.Get("cli.text.literal_10a549a45b2c");
    public static string MccMenuInventoryHint => TextResources.Get("cli.text.literal_9a36b45e04a7");
    public static string MccMenuRecipes => TextResources.Get("cli.text.literal_d664282e62e4");
    public static string MccMenuRecipesDescription => TextResources.Get("cli.text.literal_6c42f30e9aa6");
    public static string MccMenuRecipesHint => TextResources.Get("cli.text.literal_e106a897fe57");
    public static string MccMenuEntities => TextResources.Get("cli.text.literal_7fdb3ccec0e0");
    public static string MccMenuEntitiesDescription => TextResources.Get("cli.text.literal_d0c19d475c30");
    public static string MccMenuEntitiesHint => TextResources.Get("cli.text.literal_de93db002f73");
    public static string MccMenuAdvancements => TextResources.Get("cli.text.literal_d135d8be42b5");
    public static string MccMenuAdvancementsDescription => TextResources.Get("cli.text.literal_177786188ec1");
    public static string MccMenuAdvancementsHint => TextResources.Get("cli.text.literal_d090e1113d4a");
    public static string MccMenuChunks => TextResources.Get("cli.text.literal_b37e2fc666ae");
    public static string MccMenuChunksDescription => TextResources.Get("cli.text.literal_a49264deb916");
    public static string MccMenuChunksHint => TextResources.Get("cli.text.literal_7a58b275b614");
    public static string MccMenuMinimap => TextResources.Get("cli.text.literal_4bad6cff07d2");
    public static string MccMenuMinimapDescription => TextResources.Get("cli.text.literal_036ee33ecacd");
    public static string MccMenuMinimapHint => TextResources.Get("cli.text.literal_b555c0909c19");
    public static string MccMenuPlayers => TextResources.Get("cli.text.literal_92fd07450408");
    public static string MccMenuPlayersDescription => TextResources.Get("cli.text.literal_aa18f9d7270a");
    public static string MccMenuPlayersHint => TextResources.Get("cli.text.literal_7f0a1e21b931");
    public static string MccMenuScoreboard => TextResources.Get("cli.text.literal_f48570f44b23");
    public static string MccMenuScoreboardDescription => TextResources.Get("cli.text.literal_8acf446e9110");
    public static string MccMenuScoreboardHint => TextResources.Get("cli.text.literal_ce0d8a33904c");
    public static string MccMenuPlugins => TextResources.Get("cli.text.literal_9514b7ff4860");
    public static string MccMenuPluginsDescription => TextResources.Get("cli.text.literal_4d7780d70025");
    public static string MccMenuPluginsHint => TextResources.Get("cli.text.literal_f3ad4062daf9");
    public static string MccMenuMarketplaces => TextResources.Get("cli.text.literal_dfe262442ec7");
    public static string MccMenuMarketplacesDescription => TextResources.Get("cli.text.literal_0a2d2b31a5e0");
    public static string MccMenuMarketplacesHint => TextResources.Get("cli.text.literal_740efc3fa94b");
    public static string MccMenuServers => TextResources.Get("cli.text.literal_6fb2a189d815");
    public static string MccMenuServersDescription => TextResources.Get("cli.text.literal_b26ec8d25965");
    public static string MccMenuServersHint => TextResources.Get("cli.text.literal_ee3cd35fc960");
    public static string MccMenuExit => TextResources.Get("cli.text.literal_1bc4db23e417");
    public static string MccMenuExitDescription => TextResources.Get("cli.text.literal_4cd6787da191");
    public static string MccMenuExitHint => TextResources.Get("cli.text.literal_783ece18206a");
    public static string MccMenuHints => TextResources.Get("cli.text.literal_f2cda7d5cb08");
    public static string MccMenuOpenFailed(string reason) => TextResources.Format("cli.text.literal_7c9db3996a40", reason);

    // Live scoreboard tool window.
    public static string ScoreboardWindowTitle => TextResources.Get("cli.text.literal_f48570f44b23");
    public static string ScoreboardWindowEmpty => TextResources.Get("cli.text.literal_35feebc7dd4a");
    public static string ScoreboardWindowNoScores => TextResources.Get("cli.text.literal_7a2b568831f9");
    public static string ScoreboardWindowDisconnected => TextResources.Get("cli.text.literal_54d0cc40c478");
    public static string ScoreboardWindowError(string reason) => TextResources.Format("cli.text.literal_f330a88293b2", reason);
    public static string ScoreboardWindowHint => TextResources.Get("cli.text.literal_f53ba8d00194");
    public static string ScoreboardWindowObjectives(int count) => count == 1 ? TextResources.Get("cli.text.literal_7bc051be68bb") : TextResources.Format("cli.text.literal_0e9b07982161", count);
    public static string ScoreboardWindowMore(int count) => TextResources.Format("cli.text.literal_1bd67236c50a", count);

    // Command and manual browser.
    public static string BrowserTitle => TextResources.Get("cli.text.literal_070057ec8a67");
    public static string BrowserSubtitle => TextResources.Get("cli.text.literal_c85297c6699c");
    public static string BrowserCommandsTab => TextResources.Get("cli.text.literal_b269dc4e81a5");
    public static string BrowserManualTab => TextResources.Get("cli.text.literal_b0b9fe24ffa9");
    public static string BrowserInsertCommand => TextResources.Get("cli.text.literal_030829f1c604");
    public static string BrowserUsage => TextResources.Get("cli.text.literal_8d59829c1e15");
    public static string BrowserFlags => TextResources.Get("cli.text.literal_f38d9950af4f");
    public static string BrowserExamples => TextResources.Get("cli.text.literal_e68ee04dff59");
    public static string BrowserRequirements => TextResources.Get("cli.text.literal_e0cdd07f6a27");
    public static string BrowserNone => TextResources.Get("cli.text.literal_dc937b598926");
    public static string BrowserRelated => TextResources.Get("cli.text.literal_45ca56d179d4");
    public static string BrowserManualTopic => TextResources.Get("cli.text.literal_fa247349fa85");
    public static string BrowserOpenManual => TextResources.Get("cli.text.literal_04cf542f6b9f");

    // Script manager.
    public static string ScriptsUiTitle => TextResources.Get("cli.text.literal_489e6c6bb556");
    public static string ScriptsUiSubtitle => TextResources.Get("cli.text.literal_79b625bc8cb2");
    public static string ScriptsUiNew => TextResources.Get("cli.text.literal_4cf2acafe5cc");
    public static string ScriptsUiRun => TextResources.Get("cli.text.literal_00d60e31a4e6");
    public static string ScriptsUiStop => TextResources.Get("cli.text.literal_cae7d57bc067");
    public static string ScriptsUiReload => TextResources.Get("cli.text.literal_bdc090ec61e3");
    public static string ScriptsUiEdit => TextResources.Get("cli.text.literal_464c4ffd019e");
    public static string ScriptsUiLint => TextResources.Get("cli.text.literal_93d8a5e2f01a");
    public static string ScriptsUiFormat => TextResources.Get("cli.text.literal_2f343666aaa8");
    public static string ScriptsUiSettings => TextResources.Get("cli.text.literal_74a883a037bc");
    public static string ScriptsUiRepl => TextResources.Get("cli.text.literal_40f0b3ac0363");
    public static string ScriptsUiDelete => TextResources.Get("cli.text.literal_e2d0a54968ea");
    public static string ScriptsUiWatch => TextResources.Get("cli.text.literal_a71e75732446");
    public static string ScriptsUiMute => TextResources.Get("cli.text.literal_8dd6857baf02");
    public static string ScriptsUiRunning => TextResources.Get("cli.text.literal_d80ffc8dc079");
    public static string ScriptsUiStopped => TextResources.Get("cli.text.literal_133c49456b65");
    public static string ScriptsUiNoScripts => TextResources.Get("cli.text.literal_ed6c2dba26a2");
    public static string ScriptsUiTemplate => TextResources.Get("cli.text.literal_0575f29df888");
    public static string ScriptsUiId => TextResources.Get("cli.text.literal_ec2de4436bff");
    public static string ScriptsUiCreate => TextResources.Get("cli.text.literal_f16fe4b4115f");
    public static string ScriptsUiCancel => TextResources.Get("cli.text.literal_19766ed6ccb2");
    public static string ScriptsUiSource => TextResources.Get("cli.text.literal_0e570ca6fabe");
    public static string ScriptsUiSave => TextResources.Get("cli.text.literal_1509f561f241");
    public static string ScriptsUiDiscard => TextResources.Get("cli.text.literal_eb1a70e39274");
    public static string ScriptsUiOverwrite => TextResources.Get("cli.text.literal_b24963ea2cbc");
    public static string ScriptsUiReloadDisk => TextResources.Get("cli.text.literal_d4288839841c");
    public static string ScriptsUiDirty => TextResources.Get("cli.text.literal_b80012851cf0");
    public static string ScriptsUiDiagnostics => TextResources.Get("cli.text.literal_268f14bbfe11");
    public static string ScriptsUiNoDiagnostics => TextResources.Get("cli.text.literal_98f14325c58e");
    public static string ScriptsUiSaveBlocked => TextResources.Get("cli.text.literal_d0a7c8ede4ee");
    public static string ScriptsUiSaveConflict => TextResources.Get("cli.text.literal_c26693a684c8");
    public static string ScriptsUiDirtyClose => TextResources.Get("cli.text.literal_021dc3258a3a");
    public static string ScriptsUiFormatPreview => TextResources.Get("cli.text.literal_ef149ba14b11");
    public static string ScriptsUiApplyBuffer => TextResources.Get("cli.text.literal_cbd1c49223b6");
    public static string ScriptsUiFormatClean => TextResources.Get("cli.text.literal_cbb030ad09e9");
    public static string ScriptsUiSaved => TextResources.Get("cli.text.literal_fca046ccd586");
    public static string ScriptsUiSavedReload => TextResources.Get("cli.text.literal_239e4be77c97");
    public static string ScriptsUiLineColumn => TextResources.Get("cli.text.literal_216bdce038dd");
    public static string ScriptsUiInput => TextResources.Get("cli.text.literal_36ecb4f86691");
    public static string ScriptsUiOutput => TextResources.Get("cli.text.literal_b2439bcb8dee");
    public static string ScriptsUiEvaluate => TextResources.Get("cli.text.literal_966591fe7e17");
    public static string ScriptsUiSeed => TextResources.Get("cli.text.literal_eb7a919815a0");
    public static string ScriptsUiLocals => TextResources.Get("cli.text.literal_a8184dba5c5c");
    public static string ScriptsUiReplHints => TextResources.Get("cli.text.literal_5b29a829c23c");
    public static string ScriptsUiSettingsTitle => TextResources.Get("cli.text.literal_f46ee86a068c");
    public static string ScriptsUiNoSettings => TextResources.Get("cli.text.literal_09f97eb435a3");
    public static string ScriptsUiSettingsSaved => TextResources.Get("cli.text.literal_ae493e21cd43");
    public static string MgmtOn => TextResources.Get("cli.text.literal_b8d31e852725");
    public static string MgmtOff => TextResources.Get("cli.text.literal_b4dc66dde806");
    public static string ScriptsUiCount(int running, int stopped) => TextResources.Format("cli.text.literal_6c01cf3d6673", running, stopped);
    public static string ScriptsUiCaret(int line, int column) => TextResources.Format("cli.text.literal_216bdce038dd", line, column);
    public static string ScriptsUiCreated(string id) => TextResources.Format("cli.text.literal_558d3d2cf486", id);
    public static string ScriptsUiDeleteConfirm(string id) => TextResources.Format("cli.text.literal_5d94d942f99c", id);
    public static string ScriptsUiDeleteWarning(string id, bool running)
        => TextResources.Format("cli.text.literal_fe44835079a0", id)
            + (running ? TextResources.Get("cli.text.literal_9d2c8c742253") : string.Empty)
            + TextResources.Get("cli.text.literal_09934820ac06");
    public static string ScriptsUiDeleted(string id) => TextResources.Format("cli.text.literal_ba3814e14961", id);
    public static string ScriptsUiDeleteMissing(string id) => TextResources.Format("cli.text.literal_00086e26bbe9", id);
    public static string ScriptsUiFailed(string reason) => TextResources.Format("cli.text.literal_ef367c6d59fc", reason);

    // Recipe browser.
    public static string RecipeUiTitle => TextResources.Get("cli.text.literal_6af3e3548f1f");
    public static string RecipeUiSubtitle => TextResources.Get("cli.text.literal_7f6ed768d12f");
    public static string RecipeUiAll => TextResources.Get("cli.text.literal_a52ace420f21");
    public static string RecipeUiNamed => TextResources.Get("cli.text.literal_8605605ae026");
    public static string RecipeUiNumeric => TextResources.Get("cli.text.literal_36752d6cd147");
    public static string RecipeUiCraftOne => TextResources.Get("cli.text.literal_40b2b1811390");
    public static string RecipeUiCraftAll => TextResources.Get("cli.text.literal_d152a8d6b066");
    public static string RecipeUiNamedKind => TextResources.Get("cli.text.literal_9a88cf919e5e");
    public static string RecipeUiNumericKind => TextResources.Get("cli.text.literal_b50ddca27305");
    public static string RecipeUiNoRecipes => TextResources.Get("cli.text.literal_c1f83da0aa22");
    public static string RecipeUiIncomplete => TextResources.Get("cli.text.literal_83d5e87e7cce");
    public static string RecipeUiNoMenu => TextResources.Get("cli.text.literal_26fb66648f72");
    public static string RecipeUiWrongForm => TextResources.Get("cli.text.literal_ab8df759e408");
    public static string RecipeUiUnsupported => TextResources.Get("cli.text.literal_d88880ee7441");
    public static string RecipeUiPlaced => TextResources.Get("cli.text.literal_400a8f708b09");
    public static string RecipeUiRejected => TextResources.Get("cli.text.literal_34fb3e82d101");
    public static string RecipeUiInventoryLink => TextResources.Get("cli.text.literal_d664282e62e4");

    // Entity browser.
    public static string EntityUiTitle => TextResources.Get("cli.text.literal_bc17373957c3");
    public static string EntityUiSubtitle => TextResources.Get("cli.text.literal_8d5f90188a5f");
    public static string EntityUiAll => TextResources.Get("cli.text.literal_a52ace420f21");
    public static string EntityUiPlayers => TextResources.Get("cli.text.literal_84e12ac655dd");
    public static string EntityUiHostile => TextResources.Get("cli.text.literal_b3478c03ae8f");
    public static string EntityUiNeutral => TextResources.Get("cli.text.literal_1c6ac69f1e5d");
    public static string EntityUiPassive => TextResources.Get("cli.text.literal_78c04330e989");
    public static string EntityUiOther => TextResources.Get("cli.text.literal_f97e9da0e3b8");
    public static string EntityUiSortDistance => TextResources.Get("cli.text.literal_d6765360f43a");
    public static string EntityUiSortName => TextResources.Get("cli.text.literal_8c554ac73cf3");
    public static string EntityUiSortType => TextResources.Get("cli.text.literal_e794a50785f8");
    public static string EntityUiUse => TextResources.Get("cli.text.literal_c36d819e7bc6");
    public static string EntityUiAttack => TextResources.Get("cli.text.literal_4cd548f3cc29");
    public static string EntityUiNoEntities => TextResources.Get("cli.text.literal_2cb7f7a23b8e");
    public static string EntityUiGone => TextResources.Get("cli.text.literal_b8ab919b2aa7");
    public static string EntityUiNotVisible => TextResources.Get("cli.text.literal_bbc51279c8cb");
    public static string EntityUiUsed => TextResources.Get("cli.text.literal_ddb6cce313b3");
    public static string EntityUiAttacked => TextResources.Get("cli.text.literal_74e720b47e33");
    public static string EntityUiIdentity => TextResources.Get("cli.text.literal_999f23fcd7be");
    public static string EntityUiPosition => TextResources.Get("cli.text.literal_7706ead850a1");
    public static string EntityUiEquipment => TextResources.Get("cli.text.literal_ef2daf086595");
    public static string EntityUiType => TextResources.Get("cli.text.literal_baaddf70fb5d");
    public static string EntityUiName => TextResources.Get("cli.text.literal_dcd1d5223f73");
    public static string EntityUiCoordinates => TextResources.Get("cli.text.literal_117c132e939b");
    public static string EntityUiPose => TextResources.Get("cli.text.literal_f27f13d02192");
    public static string EntityUiRotation => TextResources.Get("cli.text.literal_57b5e2fc1bba");
    public static string EntityUiVelocity => TextResources.Get("cli.text.literal_8965cdc71634");
    public static string EntityUiPassengers => TextResources.Get("cli.text.literal_ae3bff5011b3");
    public static string EntityUiVehicle => TextResources.Get("cli.text.literal_a62394ba4acc");
    public static string EntityUiCarried => TextResources.Get("cli.text.literal_08a502f18362");
    public static string EntityUiEffects => TextResources.Get("cli.text.literal_358511c8c098");
    public static string EntityUiId(int id) => TextResources.Format("cli.text.literal_4e9c1df6bf02", id);
    public static string EntityUiUuid(Guid uuid) => TextResources.Format("cli.text.literal_476febb183e8", uuid);
    public static string EntityUiDistance(double distance) => TextResources.Format("cli.text.literal_92eaedf3b7ff", distance);

    // Advancements browser.
    public static string AdvancementUiTitle => TextResources.Get("cli.text.literal_e72b5fa2648c");
    public static string AdvancementUiSubtitle => TextResources.Get("cli.text.literal_0720696bf189");
    public static string AdvancementUiAll => TextResources.Get("cli.text.literal_a52ace420f21");
    public static string AdvancementUiUnlocked => TextResources.Get("cli.text.literal_531c7b28abf2");
    public static string AdvancementUiLocked => TextResources.Get("cli.text.literal_a424e33d9093");
    public static string AdvancementUiScope => TextResources.Get("cli.text.literal_b073f6c68ef8");
    public static string AdvancementUiEveryScope => TextResources.Get("cli.text.literal_93c84327ecf9");
    public static string AdvancementUiUnavailable => TextResources.Get("cli.text.literal_4ab023787049");
    public static string AdvancementUiNoMatches => TextResources.Get("cli.text.literal_0d0b006d5285");
    public static string AdvancementUiComplete => TextResources.Get("cli.text.literal_143b270a3260");
    public static string AdvancementUiIncomplete => TextResources.Get("cli.text.literal_75f33cdfc8e5");
    public static string AdvancementUiDescription => TextResources.Get("cli.text.literal_526e0087cc3f");
    public static string AdvancementUiFullId => TextResources.Get("cli.text.literal_89c3a063c5e2");
    public static string AdvancementUiFrame => TextResources.Get("cli.text.literal_d0b50e064a4f");
    public static string AdvancementUiCriteria => TextResources.Get("cli.text.literal_71708699acf8");
    public static string AdvancementUiProgress(int completed, int total) => TextResources.Format("cli.text.literal_a1a935b98150", completed, total);
    public static string AdvancementUiCriteriaProgress(int completed, int total) => $"{completed} / {total}";

    // Chunk browser.
    public static string ChunkUiTitle => TextResources.Get("cli.text.literal_9797ee183b2b");
    public static string ChunkUiSubtitle => TextResources.Get("cli.text.literal_1bfcaef4640b");
    public static string ChunkUiLoaded => TextResources.Get("cli.text.literal_2cab953f2b36");
    public static string ChunkUiUnloaded => TextResources.Get("cli.text.literal_8cc4ec25f1c0");
    public static string ChunkUiPlayer => TextResources.Get("cli.text.literal_cdb59355f3ba");
    public static string ChunkUiNoData => TextResources.Get("cli.text.literal_2993468edad6");
    public static string ChunkUiHints => TextResources.Get("cli.text.literal_4e333c2c3090");
    public static string ChunkUiSummary(
        double x, double y, double z, int chunkX, int chunkZ, int loaded, int total)
        => TextResources.Format("cli.text.literal_67ef2dcac50d", x, y, z, chunkX, chunkZ, loaded, total);
}
