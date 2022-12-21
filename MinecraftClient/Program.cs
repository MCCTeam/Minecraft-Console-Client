using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Commands;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using MinecraftClient.Scripting;
using MinecraftClient.WinAPI;
using Tomlet;
using static MinecraftClient.Settings;
using static MinecraftClient.Settings.ConsoleConfigHealper.ConsoleConfig;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.AdvancedConfig;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.GeneralConfig;

namespace MinecraftClient
{
    /// <summary>
    /// Minecraft Console Client by the MCC Team (c) 2012-2022.
    /// Allows to connect to any Minecraft server, send and receive text, automated scripts.
    /// This source code is released under the CDDL 1.0 License.
    /// </summary>
    /// <remarks>
    /// Typical steps to update MCC for a new Minecraft version
    ///  - Implement protocol changes (see Protocol18.cs)
    ///  - Handle new block types and states (see Material.cs)
    ///  - Add support for new entity types (see EntityType.cs)
    ///  - Add new item types for inventories (see ItemType.cs)
    ///  - Mark new version as handled (see ProtocolHandler.cs)
    ///  - Update MCHighestVersion field below (for versionning)
    /// </remarks>
    static class Program
    {
        private static McClient? McClient;
        public static string[]? startupargs;
        public static CultureInfo ActualCulture = CultureInfo.CurrentCulture;

        public const string Version = MCHighestVersion;
        public const string MCLowestVersion = "1.4.6";
        public const string MCHighestVersion = "1.19.2";
        public static readonly string? BuildInfo = null;

        private static bool useMcVersionOnce = false;
        private static string settingsIniPath = "MinecraftClient.ini";

        private static int CurrentThreadId;
        private static bool RestartKeepSettings = false;
        private static int RestartAfter = -1, Exitcode = 0;

        private static Task McClientInit = Task.CompletedTask;
        private static CancellationTokenSource McClientCancelTokenSource = new();

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>
        static async Task Main(string[] args)
        {
            // Take advantage of Windows 10 / Mac / Linux UTF-8 console
            if (!OperatingSystem.IsWindows() || OperatingSystem.IsWindowsVersionAtLeast(10))
                Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;

            // Setup ConsoleIO
            ConsoleIO.LogPrefix = "§8[MCC] ";
            if (args.Length >= 1 && args[^1] == "BasicIO" || args.Length >= 1 && args[^1] == "BasicIO-NoColor")
            {
                if (args.Length >= 1 && args[^1] == "BasicIO-NoColor")
                {
                    ConsoleIO.BasicIO_NoColor = true;
                }
                ConsoleIO.BasicIO = true;
                args = args.Where(o => !ReferenceEquals(o, args[^1])).ToArray();
            }

            if (!ConsoleIO.BasicIO)
                ConsoleInteractive.ConsoleWriter.Init();

            ConsoleIO.WriteLine($"Minecraft Console Client v{Version} - for MC {MCLowestVersion} to {MCHighestVersion} - Github.com/MCCTeam");

            // Build information to facilitate processing of bug reports
            if (BuildInfo != null)
                ConsoleIO.WriteLineFormatted("§8" + BuildInfo);

            string? specifiedSettingFile = null;
            if (args.Length >= 1 && File.Exists(args[0]) && Settings.ToLowerIfNeed(Path.GetExtension(args[0])) == ".ini")
            {
                specifiedSettingFile = args[0];
                // remove ini configuration file from arguments array
                string[] args_tmp = new string[args.Length - 1];
                Array.Copy(args, 1, args_tmp, 0, args.Length - 1);
                args = args_tmp;
            }

            if (HandleOtherArguments(ref args))
                return;

            // Load cached sessions from disk if necessary
            AsyncTaskHandler.CacheSessionReader = Task.Run(SessionCache.ReadCacheSessionAsync);

            // Fix issue #2119
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!ProcessConfigurationFile(specifiedSettingFile))
                return;

            // Load command-line setting arguments
            if (args.Length >= 1)
            {
                try
                {
                    Settings.LoadArguments(args);
                }
                catch (ArgumentException e)
                {
                    ConsoleIO.WriteLine(string.Format(Translations.mcc_load_from_args_fail, e.Message));
                }
            }

            // Setup exit cleaning code
            ExitCleanUp.Add(() => { DoExit(); });

            McClientInit = Task.Run(McClient.LoadCommandsAndChatbots);

            if (!string.IsNullOrWhiteSpace(Config.Main.Advanced.ConsoleTitle))
            {
                InternalConfig.Username = "New Window";
                Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);
            }

            //Test line to troubleshoot invisible colors
            if (Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLineFormatted(string.Format(Translations.debug_color_test, "[0123456789ABCDEF]: (4bit)[§00§11§22§33§44§55§66§77§88§99§aA§bB§cC§dD§eE§fF§r]"));
                Random random = new();
                { // Test 8 bit color
                    StringBuilder sb = new();
                    sb.Append("[0123456789]: (vt100 8bit)[");
                    for (int i = 0; i < 10; ++i)
                    {
                        sb.Append(ColorHelper.GetColorEscapeCode((byte)random.Next(255),
                                                                 (byte)random.Next(255),
                                                                 (byte)random.Next(255),
                                                                 true,
                                                                 ConsoleColorModeType.vt100_8bit)).Append(i);
                    }
                    sb.Append(ColorHelper.GetResetEscapeCode()).Append(']');
                    ConsoleIO.WriteLine(string.Format(Translations.debug_color_test, sb.ToString()));
                }
                { // Test 24 bit color
                    StringBuilder sb = new();
                    sb.Append("[0123456789]: (vt100 24bit)[");
                    for (int i = 0; i < 10; ++i)
                    {
                        sb.Append(ColorHelper.GetColorEscapeCode((byte)random.Next(255),
                                                                 (byte)random.Next(255),
                                                                 (byte)random.Next(255),
                                                                 true,
                                                                 ConsoleColorModeType.vt100_24bit)).Append(i);
                    }
                    sb.Append(ColorHelper.GetResetEscapeCode()).Append(']');
                    ConsoleIO.WriteLine(string.Format(Translations.debug_color_test, sb.ToString()));
                }
            }

            ConsoleIO.SuppressPrinting(true);
            // Asking the user to type in missing data such as Username and Password
            bool useBrowser = Config.Main.General.AccountType == LoginType.microsoft && Config.Main.General.Method == LoginMethod.browser;
            while (string.IsNullOrWhiteSpace(InternalConfig.Account.Login) && !useBrowser)
            {
                ConsoleIO.WriteLine(ConsoleIO.BasicIO ? Translations.mcc_login_basic_io : Translations.mcc_login, ignoreSuppress: true);
                InternalConfig.Account.Login = ConsoleIO.ReadLine().Trim();
                if (!string.IsNullOrWhiteSpace(InternalConfig.Account.Login))
                    break;
            }
            InternalConfig.Username = InternalConfig.Account.Login;

            if (string.IsNullOrWhiteSpace(InternalConfig.Account.Password) && !useBrowser
                && (Config.Main.Advanced.SessionCache == CacheType.none ||
                    (AsyncTaskHandler.CacheSessionReader != null && SessionCache.ContainsSession(InternalConfig.Account.Login))))
                RequestPassword();
            ConsoleIO.SuppressPrinting(false);

            startupargs = args;

            CurrentThreadId = Environment.CurrentManagedThreadId;

            // Check for updates
            AsyncTaskHandler.CheckUpdate = Task.Run(async () =>
            {
                bool needPromptUpdate = true;
                if (UpgradeHelper.CompareVersionInfo(Settings.Config.Head.CurrentVersion, Settings.Config.Head.LatestVersion))
                {
                    needPromptUpdate = false;
                    ConsoleIO.WriteLineFormatted("§e" + string.Format(Translations.mcc_has_update, UpgradeHelper.GithubReleaseUrl), true);
                }
                await Task.Delay(20 * 1000);
                await UpgradeHelper.DoCheckUpdate(CancellationToken.None);
                if (needPromptUpdate)
                {
                    if (UpgradeHelper.CompareVersionInfo(Settings.Config.Head.CurrentVersion, Settings.Config.Head.LatestVersion))
                    {
                        ConsoleIO.WriteLineFormatted("§e" + string.Format(Translations.mcc_has_update, UpgradeHelper.GithubReleaseUrl), true);
                    }
                }
            });

            HttpClient loginHttpClient = new();

            while (true)
            {
                try { await InitializeClient(loginHttpClient); }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted("§c" + Translations.mcc_unhandled_exception);
                    ConsoleIO.WriteLine($"{e.GetType().Name}:{e.GetFullMessage()}");
                    string? stackTrace = e.StackTrace;
                    if (stackTrace != null)
                        ConsoleIO.WriteLine(stackTrace);
                }

                if (McClient != null)
                {
                    McClient.Disconnect();
                    ConsoleIO.Reset();
                    McClient = null;
                }

                if (RestartAfter < 0 && FailureInfo.hasFailure)
                    RestartAfter = HandleFailure();

                if (RestartAfter < 0)
                    break;

                if (RestartAfter > 0)
                {
                    ConsoleIO.WriteLine(string.Format(Translations.mcc_restart_delay, (double)RestartAfter / 1000.0));
                    Thread.Sleep(RestartAfter);
                }

                ConsoleIO.WriteLine(Translations.mcc_restart);

                ReloadSettings(RestartKeepSettings);

                Exitcode = 0;
                RestartAfter = -1;
                RestartKeepSettings = false;
                FailureInfo.hasFailure = false;
                McClientCancelTokenSource = new CancellationTokenSource();
            }

            DoExit();
        }

        /// <summary>
        /// Handles command line arguments other than settings.
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <returns>Whether the program needs to be exited.</returns>
        /// <exception cref="ArgumentException"></exception>
        private static bool HandleOtherArguments(ref string[] args)
        {
            // Debug input ?
            if (args.Length == 1 && args[0] == "--keyboard-debug")
            {
                ConsoleIO.WriteLine("Keyboard debug mode: Press any key to display info");
                ConsoleIO.DebugReadInput();
                return true;
            }

            // Other command-line arguments
            if (args.Length >= 1)
            {
                if (args.Contains("--help"))
                {
                    Console.WriteLine("Command-Line Help:");
                    Console.WriteLine("MinecraftClient.exe <username> <password> <server>");
                    Console.WriteLine("MinecraftClient.exe <username> <password> <server> \"/mycommand\"");
                    Console.WriteLine("MinecraftClient.exe --setting=value [--other settings]");
                    Console.WriteLine("MinecraftClient.exe --section.setting=value [--other settings]");
                    Console.WriteLine("MinecraftClient.exe <settings-file.ini> [--other settings]");
                    return true;
                }

                if (args.Contains("--upgrade"))
                {
                    UpgradeHelper.HandleBlockingUpdate(forceUpgrade: false);
                    return true;
                }

                if (args.Contains("--force-upgrade"))
                {
                    UpgradeHelper.HandleBlockingUpdate(forceUpgrade: true);
                    return true;
                }

                if (args.Contains("--generate"))
                {
                    string dataGenerator = "";
                    string dataPath = "";

                    foreach (string argument in args)
                    {
                        if (argument.StartsWith("--") && !argument.Contains("--generate"))
                        {
                            if (!argument.Contains('='))
                                throw new ArgumentException(string.Format(Translations.error_setting_argument_syntax, argument));

                            string[] argParts = argument[2..].Split('=');
                            string argName = argParts[0].Trim();
                            string argValue = argParts[1].Replace("\"", "").Trim();

                            if (argName == "data-path")
                            {
                                Console.WriteLine(dataPath);
                                dataPath = argValue;
                            }

                            if (argName == "data-generator")
                            {
                                dataGenerator = argValue;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(dataGenerator) || !(Settings.ToLowerIfNeed(dataGenerator).Equals("entity") || Settings.ToLowerIfNeed(dataGenerator).Equals("item") || Settings.ToLowerIfNeed(dataGenerator).Equals("block")))
                    {
                        Console.WriteLine(Translations.error_generator_invalid);
                        Console.WriteLine(Translations.error_usage + " MinecraftClient.exe --data-generator=<entity|item|block> --data-path=\"<path to Translations.json>\"");
                        return true;
                    }

                    if (string.IsNullOrEmpty(dataPath))
                    {
                        Console.WriteLine(string.Format(Translations.error_missing_argument, "--data-path"));
                        Console.WriteLine(Translations.error_usage + " MinecraftClient.exe --data-generator=<entity|item|block> --data-path=\"<path to Translations.json>\"");
                        return true;
                    }

                    if (!File.Exists(dataPath))
                    {
                        Console.WriteLine(string.Format(Translations.error_generator_path, dataPath));
                        return true;
                    }

                    if (!dataPath.EndsWith(".json"))
                    {
                        Console.WriteLine(string.Format(Translations.error_generator_json, dataPath));
                        return true;
                    }

                    Console.WriteLine(string.Format(Translations.mcc_generator_generating, dataGenerator, dataPath));

                    switch (dataGenerator)
                    {
                        case "entity":
                            EntityPaletteGenerator.GenerateEntityTypes(dataPath);
                            break;

                        case "item":
                            ItemPaletteGenerator.GenerateItemType(dataPath);
                            break;

                        case "block":
                            BlockPaletteGenerator.GenerateBlockPalette(dataPath);
                            break;
                    }

                    Console.WriteLine(string.Format(Translations.mcc_generator_done, dataGenerator, dataPath));
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Arguments</param>
        /// <returns>Whether the program needs to be exited.</returns>
        private static bool ProcessConfigurationFile(string? specifiedSettingFile)
        {
            bool loadSucceed, needWriteDefaultSetting, newlyGenerated = false;
            if (!string.IsNullOrEmpty(specifiedSettingFile))
            {
                (loadSucceed, needWriteDefaultSetting) = Settings.LoadFromFile(specifiedSettingFile);
                settingsIniPath = specifiedSettingFile;
            }
            else if (File.Exists("MinecraftClient.ini"))
            {
                (loadSucceed, needWriteDefaultSetting) = Settings.LoadFromFile("MinecraftClient.ini");
            }
            else
            {
                loadSucceed = true;
                needWriteDefaultSetting = true;
                newlyGenerated = true;
            }

            if (needWriteDefaultSetting)
            {
                Config.Main.Advanced.Language = Settings.GetDefaultGameLanguage();
                _ = WriteBackSettings(false);
                if (newlyGenerated)
                    ConsoleIO.WriteLineFormatted("§c" + string.Format(Translations.mcc_settings_generated, Path.GetFullPath(settingsIniPath)));
                ConsoleIO.WriteLine(Translations.mcc_run_with_default_settings);
            }
            else if (!loadSucceed)
            {
                ConsoleInteractive.ConsoleReader.StopReadThread();
                while (true)
                {
                    ConsoleIO.WriteLine(string.Empty);
                    ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_invaild_config, Config.Main.Advanced.InternalCmdChar.ToLogString()));
                    ConsoleIO.WriteLineFormatted(Translations.mcc_press_exit, acceptnewlines: true);
                    string command = ConsoleInteractive.ConsoleReader.RequestImmediateInput().Trim();
                    if (command.Length > 0)
                    {
                        if (Config.Main.Advanced.InternalCmdChar != InternalCmdCharType.none
                            && command[0] == Config.Main.Advanced.InternalCmdChar.ToChar())
                            command = command[1..];

                        if (command.StartsWith("exit") || command.StartsWith("quit"))
                        {
                            return false;
                        }

                        if (command.StartsWith("new"))
                        {
                            Config.Main.Advanced.Language = Settings.GetDefaultGameLanguage();
                            _ = WriteBackSettings(true);
                            ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_gen_new_config, settingsIniPath));
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                //Load external translation file. Should be called AFTER settings loaded
                if (!Config.Main.Advanced.Language.StartsWith("en"))
                    ConsoleIO.WriteLine(string.Format(Translations.mcc_help_us_translate, Settings.TranslationProjectUrl));
                _ = WriteBackSettings(true); // format
            }
            return true;
        }

        /// <summary>
        /// Reduest user to submit password.
        /// </summary>
        private static void RequestPassword()
        {
            ConsoleIO.WriteLine(ConsoleIO.BasicIO ? string.Format(Translations.mcc_password_basic_io, InternalConfig.Account.Login) + "\n" : Translations.mcc_password_hidden, ignoreSuppress: true);
            string? password = ConsoleIO.BasicIO ? Console.ReadLine() : ConsoleIO.ReadPassword();
            if (string.IsNullOrWhiteSpace(password))
                InternalConfig.Account.Password = "-";
            else
                InternalConfig.Account.Password = password;
        }

        private static async Task<Tuple<ProtocolHandler.LoginResult, SessionToken?, PlayerKeyPair?>> LoginAsync(HttpClient httpClient)
        {
            SessionToken? session;
            PlayerKeyPair? playerKeyPair;
            ProtocolHandler.LoginResult result = ProtocolHandler.LoginResult.LoginRequired;

            if (InternalConfig.Account.Password == "-")
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_offline, acceptnewlines: true);
                result = ProtocolHandler.LoginResult.Success;
                session = new()
                {
                    PlayerID = "0",
                    PlayerName = InternalConfig.Username
                };
                playerKeyPair = null;
            }
            else
            {
                await AsyncTaskHandler.CacheSessionReader;
                (session, playerKeyPair) = SessionCache.GetSession(InternalConfig.Account.Login);

                // Validate cached session or login new session.
                if (Config.Main.Advanced.SessionCache != CacheType.none && session != null)
                {
                    result = await ProtocolHandler.GetTokenValidation(session);
                    if (result != ProtocolHandler.LoginResult.Success)
                    {
                        ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_session_invalid, acceptnewlines: true);
                        // Try to refresh access token
                        if (!string.IsNullOrWhiteSpace(session.RefreshToken))
                        {
                            try
                            {
                                (result, session) = await ProtocolHandler.MicrosoftLoginRefreshAsync(httpClient, session.RefreshToken);
                            }
                            catch (Exception ex)
                            {
                                ConsoleIO.WriteLine("Refresh access token fail: " + ex.Message);
                                result = ProtocolHandler.LoginResult.InvalidResponse;
                            }
                        }

                        if (result != ProtocolHandler.LoginResult.Success
                            && string.IsNullOrWhiteSpace(InternalConfig.Account.Password)
                            && !(Config.Main.General.AccountType == LoginType.microsoft && Config.Main.General.Method == LoginMethod.browser))
                        {
                            ConsoleIO.SuppressPrinting(true);
                            RequestPassword();
                            ConsoleIO.SuppressPrinting(false);
                        }
                    }
                    else
                    {
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_session_valid, session.PlayerName));
                    }
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    ConsoleIO.WriteLine(string.Format(Translations.mcc_connecting, Config.Main.General.AccountType == LoginType.mojang ? "Minecraft.net" : "Microsoft"));
                    (result, session) = await ProtocolHandler.GetLoginAsync(httpClient, InternalConfig.Account.Login, InternalConfig.Account.Password, Config.Main.General.AccountType);
                }

                if (result == ProtocolHandler.LoginResult.Success && session != null)
                {
                    var serverInfo = SessionCache.GetServerInfo($"{InternalConfig.ServerIP}:{InternalConfig.ServerPort}");
                    if (serverInfo != null && serverInfo.ServerPublicKey != null)
                    {
                        try
                        {
                            byte[] key = Crypto.CryptoHandler.ClientAESPrivateKey = Crypto.CryptoHandler.GenerateAESPrivateKey();
                            session.ServerInfoHash = Crypto.CryptoHandler.GetServerHash(serverInfo.ServerIDhash!, serverInfo.ServerPublicKey!, key);
                            session.SessionPreCheckTask = ProtocolHandler.SessionCheckAsync(httpClient, session.PlayerID, session.ID, session.ServerInfoHash);
                        }
                        catch (ArgumentException) { }
                    }
                }
            }

            return new(result, session, playerKeyPair);
        }

        private static async Task<PlayerKeyPair?> RefreshPlayerKeyPair(HttpClient httpClient, Task<Tuple<ProtocolHandler.LoginResult, SessionToken?, PlayerKeyPair?>> loginTask)
        {
            (ProtocolHandler.LoginResult loginResult, SessionToken? session, PlayerKeyPair? playerKeyPair) = await loginTask;

            if (loginResult != ProtocolHandler.LoginResult.Success || session == null || string.IsNullOrEmpty(session.ID))
                return null;

            bool needRefresh = true;
            if (Config.Main.Advanced.ProfileKeyCache != CacheType.none && playerKeyPair != null)
            {
                needRefresh = playerKeyPair.NeedRefresh();
                if (needRefresh)
                    ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_profile_key_invalid, acceptnewlines: true);
                else
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_profile_key_valid, session.PlayerName));
            }

            if (playerKeyPair == null || needRefresh)
            {
                ConsoleIO.WriteLineFormatted(Translations.mcc_fetching_key, acceptnewlines: true);
                playerKeyPair = await Protocol.Microsoft.RequestProfileKeyAsync(httpClient, session.ID);
            }

            return playerKeyPair;
        }

        private static async Task<Tuple<bool, int, ForgeInfo?>> GetServerInfoAsync(HttpClient httpClient, Task<Tuple<ProtocolHandler.LoginResult, SessionToken?, PlayerKeyPair?>> loginTask)
        {
            bool isRealms = false;
            if (string.IsNullOrWhiteSpace(InternalConfig.ServerIP))
            {
                ConsoleIO.SuppressPrinting(true);
                ConsoleIO.WriteLine(Translations.mcc_ip, ignoreSuppress: true);
                string addressInput = ConsoleIO.ReadLine();
                ConsoleIO.SuppressPrinting(false);

                if (addressInput.StartsWith("realms:"))
                {
                    if (Config.Main.Advanced.MinecraftRealms)
                    {
                        (ProtocolHandler.LoginResult loginResult, SessionToken? session, _) = await loginTask;
                        if (loginResult == ProtocolHandler.LoginResult.Success && session != null && !string.IsNullOrEmpty(session.ID))
                        {
                            List<string> availableWorlds = await ProtocolHandler.RealmsListWorldsAsync(httpClient, InternalConfig.Username, session.PlayerID, session.ID);
                            if (availableWorlds.Count == 0)
                            {
                                FailureInfo.Record(Translations.error_realms_access_denied, false, ChatBot.DisconnectReason.LoginRejected);
                                return new(false, 0, null);
                            }
                            string worldId = addressInput.Split(':')[1];
                            if (!availableWorlds.Contains(worldId) && int.TryParse(worldId, NumberStyles.Any, CultureInfo.CurrentCulture, out int worldIndex) && worldIndex < availableWorlds.Count)
                                worldId = availableWorlds[worldIndex];
                            if (availableWorlds.Contains(worldId))
                            {
                                string RealmsAddress = await ProtocolHandler.GetRealmsWorldServerAddress(httpClient, worldId, InternalConfig.Username, session.PlayerID, session.ID);
                                if (!string.IsNullOrEmpty(RealmsAddress))
                                {
                                    addressInput = RealmsAddress;
                                    isRealms = true;
                                    InternalConfig.MinecraftVersion = MCHighestVersion;
                                }
                                else
                                {
                                    FailureInfo.Record(Translations.error_realms_server_unavailable, false, ChatBot.DisconnectReason.LoginRejected);
                                    return new(false, 0, null);
                                }
                            }
                            else
                            {
                                FailureInfo.Record(Translations.error_realms_server_id, false, ChatBot.DisconnectReason.LoginRejected);
                                return new(false, 0, null);
                            }
                        }
                        else
                        {
                            FailureInfo.Record(Translations.error_realms_disabled, false, null);
                            return new(false, 0, null);
                        }
                    }
                    else
                    {
                        FailureInfo.Record(Translations.error_realms_disabled, false, null);
                        return new(false, 0, null);
                    }
                }
                Config.Main.SetServerIP(new MainConfigHealper.MainConfig.ServerInfoConfig(addressInput), true);
            }

            // Get server version
            int protocolversion = 0;
            ForgeInfo? forgeInfo = null;

            if (!string.IsNullOrEmpty(InternalConfig.MinecraftVersion) && !string.Equals(InternalConfig.MinecraftVersion, "auto", StringComparison.InvariantCultureIgnoreCase))
            {
                protocolversion = ProtocolHandler.MCVer2ProtocolVersion(InternalConfig.MinecraftVersion);

                if (protocolversion != 0)
                    ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_use_version, InternalConfig.MinecraftVersion, protocolversion));
                else
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_unknown_version, InternalConfig.MinecraftVersion), acceptnewlines: true);

                if (useMcVersionOnce)
                {
                    useMcVersionOnce = false;
                    InternalConfig.MinecraftVersion = string.Empty;
                }
            }

            // Retrieve server info if version is not manually set OR if need to retrieve Forge information
            if (!isRealms && (protocolversion == 0 || (Config.Main.Advanced.EnableForge == ForgeConfigType.auto) ||
                ((Config.Main.Advanced.EnableForge == ForgeConfigType.force) && !ProtocolHandler.ProtocolMayForceForge(protocolversion))))
            {
                ConsoleIO.WriteLine(protocolversion == 0 ? Translations.mcc_retrieve : Translations.mcc_forge);
                (bool status, protocolversion, forgeInfo) = await ProtocolHandler.GetServerInfoAsync(InternalConfig.ServerIP, InternalConfig.ServerPort, protocolversion);
                if (!status)
                {
                    FailureInfo.Record(Translations.error_ping, true, ChatBot.DisconnectReason.ConnectionLost);
                    return new(false, 0, null);
                }
            }

            // Force-enable Forge support?
            if (!isRealms && (Config.Main.Advanced.EnableForge == ForgeConfigType.force) && forgeInfo == null)
            {
                if (ProtocolHandler.ProtocolMayForceForge(protocolversion))
                {
                    ConsoleIO.WriteLine(Translations.mcc_forgeforce);
                    forgeInfo = ProtocolHandler.ProtocolForceForge(protocolversion);
                }
                else
                {
                    FailureInfo.Record(Translations.error_forgeforce, true, ChatBot.DisconnectReason.ConnectionLost);
                    return new(false, 0, null);
                }
            }

            return new(true, protocolversion, forgeInfo);
        }

        private static async Task SaveSession(Task<PlayerKeyPair?> refreshPlayerKeyTask, Task<Tuple<ProtocolHandler.LoginResult, SessionToken?, PlayerKeyPair?>> loginTask)
        {
            if (Config.Main.Advanced.SessionCache != CacheType.none)
            {
                (ProtocolHandler.LoginResult loginResult, SessionToken? session, _) = await loginTask;
                if (loginResult == ProtocolHandler.LoginResult.Success && session != null && !string.IsNullOrEmpty(session.ID))
                {
                    PlayerKeyPair? playerKeyPair = await refreshPlayerKeyTask;
                    await SessionCache.StoreSessionAsync(InternalConfig.Account.Login, session, playerKeyPair);
                }
            }
        }

        /// <summary>
        /// Start a new Client
        /// </summary>
        private async static Task InitializeClient(HttpClient loginHttpClient)
        {
            InternalConfig.MinecraftVersion = Config.Main.Advanced.MinecraftVersion;

            SessionToken? session;
            PlayerKeyPair? playerKeyPair;
            ProtocolHandler.LoginResult result;

            var loginTask = LoginAsync(loginHttpClient);
            var getServerInfoTask = GetServerInfoAsync(loginHttpClient, loginTask);
            var refreshPlayerKeyTask = RefreshPlayerKeyPair(loginHttpClient, loginTask);

            (result, session, playerKeyPair) = await loginTask;
            if (result == ProtocolHandler.LoginResult.Success && session != null)
            {
                InternalConfig.Username = session.PlayerName;

                if (!string.IsNullOrWhiteSpace(Config.Main.Advanced.ConsoleTitle))
                    Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);

                if (Config.Main.Advanced.PlayerHeadAsIcon && OperatingSystem.IsWindows())
                    _ = Task.Run(async () => { await ConsoleIcon.SetPlayerIconAsync(loginHttpClient, InternalConfig.Username); });

                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLine(string.Format(Translations.debug_session_id, session.ID));

                (bool status, int protocolversion, ForgeInfo? forgeInfo) = await getServerInfoTask;
                if (!status)
                    return;

                if (Config.Main.General.AccountType == LoginType.microsoft
                    && (InternalConfig.Account.Password != "-" || Config.Main.General.Method == LoginMethod.browser)
                    && Config.Signature.LoginWithSecureProfile
                    && protocolversion >= 759 /* 1.19 and above */)
                {
                    playerKeyPair = await refreshPlayerKeyTask;
                }

                // Proceed to server login
                if (protocolversion != 0)
                {
                    try
                    {
                        await McClientInit;
                        McClient = new McClient(InternalConfig.ServerIP, InternalConfig.ServerPort, McClientCancelTokenSource);

                        // Start the main TCP client
                        await McClient.Login(loginHttpClient, session, playerKeyPair, protocolversion, forgeInfo);
                    }
                    catch (NotSupportedException)
                    {
                        FailureInfo.Record(Translations.error_unsupported, true);
                        return;
                    }
                    catch (Exception e)
                    {
                        FailureInfo.Record(e.Message, false, ChatBot.DisconnectReason.ConnectionLost);
                        return;
                    }

                    await AsyncTaskHandler.SaveSessionToDisk;
                    AsyncTaskHandler.SaveSessionToDisk = Task.Run(async () => { await SaveSession(refreshPlayerKeyTask, loginTask); });

                    // Update console title
                    if (!string.IsNullOrWhiteSpace(Config.Main.Advanced.ConsoleTitle))
                        Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);

                    await McClient.StartUpdating();
                }
                else
                {
                    FailureInfo.Record(Translations.error_determine, true);
                    return;
                }
            }
            else
            {
                string failureMessage = Translations.error_login + result switch
                {
#pragma warning disable format // @formatter:off
                    ProtocolHandler.LoginResult.AccountMigrated     =>  Translations.error_login_migrated,
                    ProtocolHandler.LoginResult.ServiceUnavailable  =>  Translations.error_login_server,
                    ProtocolHandler.LoginResult.WrongPassword       =>  Translations.error_login_blocked,
                    ProtocolHandler.LoginResult.InvalidResponse     =>  Translations.error_login_response,
                    ProtocolHandler.LoginResult.NotPremium          =>  Translations.error_login_premium,
                    ProtocolHandler.LoginResult.OtherError          =>  Translations.error_login_network,
                    ProtocolHandler.LoginResult.SSLError            =>  Translations.error_login_ssl,
                    ProtocolHandler.LoginResult.UserCancel          =>  Translations.error_login_cancel,
                    _                                               =>  Translations.error_login_unknown,
#pragma warning restore format // @formatter:on
                };
                FailureInfo.Record(failureMessage, false, ChatBot.DisconnectReason.LoginRejected);
                return;
            }
        }

        public static int GetMainThreadId()
        {
            return CurrentThreadId;
        }

        /// <summary>
        /// Reloads settings
        /// </summary>
        public static void ReloadSettings(bool keepAccountAndServerSettings = false)
        {
            if (Settings.LoadFromFile(settingsIniPath, keepAccountAndServerSettings).Item1)
                ConsoleIO.WriteLine(string.Format(Translations.config_load, settingsIniPath));
        }

        /// <summary>
        /// Write-back settings
        /// </summary>
        public static Task WriteBackSettings(bool enableBackup = true)
        {
            return Task.Run(async () =>
            {
                await AsyncTaskHandler.WritebackSettingFile;
                AsyncTaskHandler.WritebackSettingFile = Settings.WriteToFileAsync(settingsIniPath, enableBackup);
            });
        }

        /// <summary>
        /// Disconnect the current client from the server and restart it
        /// </summary>
        /// <param name="delaySeconds">Optional delay, in seconds, before restarting</param>
        public static void SetRestart(int delayMilliseconds = 0, bool keepAccountAndServerSettings = false)
        {
            RestartAfter = Math.Max(0, delayMilliseconds);
            RestartKeepSettings = keepAccountAndServerSettings;
            McClientCancelTokenSource.Cancel();
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>
        public static void SetExit(int exitcode = 0, bool handleFailure = false)
        {
            RestartAfter = -1;
            Exitcode = exitcode;
            if (handleFailure)
                FailureInfo.Record();
            McClientCancelTokenSource.Cancel();
        }

        public static void DoExit()
        {
            ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
            WriteBackSettings(true).Wait();
            ConsoleIO.WriteLineFormatted("§a" + string.Format(Translations.config_saving, settingsIniPath));

            if (McClient != null) { McClient.Disconnect(); ConsoleIO.Reset(); }
            if (Config.Main.Advanced.PlayerHeadAsIcon) { ConsoleIcon.RevertToMCCIcon(); }

            AsyncTaskHandler.ExitCleanUp();
            Environment.Exit(Exitcode);
        }

        /// <summary>
        /// Handle fatal errors such as ping failure, login failure, server disconnection, and so on.
        /// Allows AutoRelog to perform on fatal errors, prompt for server version, and offline commands.
        /// </summary>
        /// <param name="errorMessage">Error message to display and optionally pass to AutoRelog bot</param>
        /// <param name="versionError">Specify if the error is related to an incompatible or unkown server version</param>
        /// <param name="disconnectReason">If set, the error message will be processed by the AutoRelog bot</param>
        public static int HandleFailure()
        {
            if (!string.IsNullOrEmpty(FailureInfo.errorMessage))
            {
                ConsoleIO.Reset();
                while (Console.KeyAvailable)
                    Console.ReadKey(true);
                ConsoleIO.WriteLine(FailureInfo.errorMessage);

                if (FailureInfo.disconnectReason.HasValue)
                {
                    int autoRelogResult = ChatBots.AutoRelog.OnDisconnectStatic(FailureInfo.disconnectReason.Value, FailureInfo.errorMessage);
                    if (autoRelogResult >= 0)
                        return autoRelogResult; //AutoRelog is triggering a restart of the client
                }
            }

            if (InternalConfig.InteractiveMode)
            {
                if (FailureInfo.versionError)
                {
                    ConsoleIO.WriteLine(Translations.mcc_server_version);
                    InternalConfig.MinecraftVersion = ConsoleInteractive.ConsoleReader.RequestImmediateInput();
                    if (!string.IsNullOrEmpty(InternalConfig.MinecraftVersion))
                    {
                        useMcVersionOnce = true;
                        return 0;
                    }
                }

                ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_disconnected, Config.Main.Advanced.InternalCmdChar.ToLogString()));
                ConsoleIO.WriteLineFormatted(Translations.mcc_press_exit, acceptnewlines: true);

                while (true)
                {
                    string command = ConsoleInteractive.ConsoleReader.RequestImmediateInput().Trim();
                    if (string.IsNullOrEmpty(command))
                    {
                        return -1;
                    }
                    else
                    {
                        if (Config.Main.Advanced.InternalCmdChar != InternalCmdCharType.none
                            && command[0] == Config.Main.Advanced.InternalCmdChar.ToChar())
                            command = command[1..];

                        if (command.StartsWith("reco"))
                        {
                            string message = Commands.Reco.DoReconnect(Config.AppVar.ExpandVars(command));
                            if (string.IsNullOrEmpty(message))
                            {
                                ConsoleIO.WriteLine(string.Empty);
                                RestartKeepSettings = true;
                                return 0;
                            }
                            else
                                ConsoleIO.WriteLineFormatted("§8MCC: " + message);
                        }
                        else if (command.StartsWith("connect"))
                        {
                            string message = Commands.Connect.DoConnect(Config.AppVar.ExpandVars(command));
                            if (string.IsNullOrEmpty(message))
                            {
                                ConsoleIO.WriteLine(string.Empty);
                                RestartKeepSettings = true;
                                return 0;
                            }
                            else
                                ConsoleIO.WriteLineFormatted("§8MCC: " + message);
                        }
                        else if (command.StartsWith("exit") || command.StartsWith("quit"))
                        {
                            return -1;
                        }
                        else if (command.StartsWith("help"))
                        {
                            ConsoleIO.WriteLineFormatted("§8MCC: " + new Commands.Reco().GetCmdDescTranslated(ListAllUsage: false));
                            ConsoleIO.WriteLineFormatted("§8MCC: " + new Commands.Connect().GetCmdDescTranslated(ListAllUsage: false));
                        }
                        else
                        {
                            ConsoleIO.WriteLineFormatted("§8MCC: " + string.Format(Translations.icmd_unknown, command.Split(' ')[0]));
                        }
                    }
                }
            }
            else
            {
                // Not in interactive mode, just exit and let the calling script handle the failure
                if (FailureInfo.disconnectReason.HasValue)
                {
                    // Return distinct exit codes for known failures.
                    if (FailureInfo.disconnectReason.Value == ChatBot.DisconnectReason.UserLogout) Exitcode = 1;
                    if (FailureInfo.disconnectReason.Value == ChatBot.DisconnectReason.InGameKick) Exitcode = 2;
                    if (FailureInfo.disconnectReason.Value == ChatBot.DisconnectReason.ConnectionLost) Exitcode = 3;
                    if (FailureInfo.disconnectReason.Value == ChatBot.DisconnectReason.LoginRejected) Exitcode = 4;
                }
                return -1;
            }

        }

        /// <summary>
        /// Enumerate types in namespace through reflection
        /// </summary>
        /// <param name="nameSpace">Namespace to process</param>
        /// <param name="assembly">Assembly to use. Default is Assembly.GetExecutingAssembly()</param>
        /// <returns></returns>
        public static Type[] GetTypesInNamespace(string nameSpace, Assembly? assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetExecutingAssembly(); }
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

        /// <summary>
        /// Static initialization of build information, read from assembly information
        /// </summary>
        static Program()
        {
            if (typeof(Program)
                .Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
                .FirstOrDefault() is AssemblyConfigurationAttribute attribute)
                BuildInfo = attribute.Configuration;
        }

        private static class FailureInfo
        {
            public static bool hasFailure = false;

            public static string? errorMessage = null;
            public static bool versionError = false;
            public static ChatBot.DisconnectReason? disconnectReason = null;

            public static void Record(string? errorMessage = null, bool versionError = false, ChatBot.DisconnectReason? disconnectReason = null)
            {
                FailureInfo.hasFailure = true;
                FailureInfo.errorMessage = errorMessage;
                FailureInfo.versionError = versionError;
                FailureInfo.disconnectReason = disconnectReason;
            }
        }
    }
}
