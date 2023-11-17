using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
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
        private static McClient? client;
        public static string[]? startupargs;
        public static CultureInfo ActualCulture = CultureInfo.CurrentCulture;

        public const string Version = MCHighestVersion;
        public const string MCLowestVersion = "1.4.6";
        public const string MCHighestVersion = "1.20.2";
        public static readonly string? BuildInfo = null;

        private static Tuple<Thread, CancellationTokenSource>? offlinePrompt = null;
        private static bool useMcVersionOnce = false;
        private static string settingsIniPath = "MinecraftClient.ini";

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>
        static void Main(string[] args)
        {
            Task.Run(() =>
            {
                // "ToLower" require "CultureInfo" to be initialized on first run, which can take a lot of time.
                _ = "a".ToLower();

                //Take advantage of Windows 10 / Mac / Linux UTF-8 console
                if (OperatingSystem.IsWindows())
                {
                    // If we're on windows, check if our version is Win10 or greater.
                    if (OperatingSystem.IsWindowsVersionAtLeast(10))
                        Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
                }
                else
                {
                    // Apply to all other operating systems.
                    Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
                }

                // Fix issue #2119
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            });

            //Setup ConsoleIO
            ConsoleIO.LogPrefix = "§8[MCC] ";
            if (args.Length >= 1 && args[^1] == "BasicIO" || args.Length >= 1 && args[^1] == "BasicIO-NoColor")
            {
                if (args.Length >= 1 && args[^1] == "BasicIO-NoColor")
                {
                    ConsoleIO.BasicIO_NoColor = true;
                }
                ConsoleIO.BasicIO = true;
                args = args.Where(o => !Object.ReferenceEquals(o, args[^1])).ToArray();
            }

            if (!ConsoleIO.BasicIO)
                ConsoleInteractive.ConsoleWriter.Init();

            ConsoleIO.WriteLine($"Minecraft Console Client v{Version} - for MC {MCLowestVersion} to {MCHighestVersion} - Github.com/MCCTeam");

            //Build information to facilitate processing of bug reports
            if (BuildInfo != null)
                ConsoleIO.WriteLineFormatted("§8" + BuildInfo);

            //Debug input ?
            if (args.Length == 1 && args[0] == "--keyboard-debug")
            {
                ConsoleIO.WriteLine("Keyboard debug mode: Press any key to display info");
                ConsoleIO.DebugReadInput();
            }

            //Process ini configuration file
            {
                bool loadSucceed, needWriteDefaultSetting, newlyGenerated = false;
                if (args.Length >= 1 && File.Exists(args[0]) && Settings.ToLowerIfNeed(Path.GetExtension(args[0])) == ".ini")
                {
                    (loadSucceed, needWriteDefaultSetting) = Settings.LoadFromFile(args[0]);
                    settingsIniPath = args[0];

                    //remove ini configuration file from arguments array
                    List<string> args_tmp = args.ToList<string>();
                    args_tmp.RemoveAt(0);
                    args = args_tmp.ToArray();
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
                    WriteBackSettings(false);
                    if (newlyGenerated)
                        ConsoleIO.WriteLineFormatted("§c" + Translations.mcc_settings_generated);
                    ConsoleIO.WriteLine(Translations.mcc_run_with_default_settings);
                }
                else if (!loadSucceed)
                {
                    ConsoleInteractive.ConsoleReader.StopReadThread();
                    string command = " ";
                    while (command.Length > 0)
                    {
                        ConsoleIO.WriteLine(string.Empty);
                        ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_invaild_config, Config.Main.Advanced.InternalCmdChar.ToLogString()));
                        ConsoleIO.WriteLineFormatted(Translations.mcc_press_exit, acceptnewlines: true);
                        command = ConsoleInteractive.ConsoleReader.RequestImmediateInput().Trim();
                        if (command.Length > 0)
                        {
                            if (Config.Main.Advanced.InternalCmdChar.ToChar() != ' '
                                && command[0] == Config.Main.Advanced.InternalCmdChar.ToChar())
                                command = command[1..];

                            if (command.StartsWith("exit") || command.StartsWith("quit"))
                            {
                                return;
                            }
                            else if (command.StartsWith("new"))
                            {
                                Config.Main.Advanced.Language = Settings.GetDefaultGameLanguage();
                                WriteBackSettings(true);
                                ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_gen_new_config, settingsIniPath));
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    return;
                }
                else
                {
                    //Load external translation file. Should be called AFTER settings loaded
                    if (!Config.Main.Advanced.Language.StartsWith("en"))
                        ConsoleIO.WriteLine(string.Format(Translations.mcc_help_us_translate, Settings.TranslationProjectUrl));
                    WriteBackSettings(true); // format
                }
            }

            //Other command-line arguments
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
                    return;
                }

                if (args.Contains("--upgrade"))
                {
                    UpgradeHelper.HandleBlockingUpdate(forceUpgrade: false);
                    return;
                }

                if (args.Contains("--force-upgrade"))
                {
                    UpgradeHelper.HandleBlockingUpdate(forceUpgrade: true);
                    return;
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
                        return;
                    }

                    if (string.IsNullOrEmpty(dataPath))
                    {
                        Console.WriteLine(string.Format(Translations.error_missing_argument, "--data-path"));
                        Console.WriteLine(Translations.error_usage + " MinecraftClient.exe --data-generator=<entity|item|block> --data-path=\"<path to Translations.json>\"");
                        return;
                    }

                    if (!File.Exists(dataPath))
                    {
                        Console.WriteLine(string.Format(Translations.error_generator_path, dataPath));
                        return;
                    }

                    if (!dataPath.EndsWith(".json"))
                    {
                        Console.WriteLine(string.Format(Translations.error_generator_json, dataPath));
                        return;
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
                    return;
                }
            }

            if (OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(Config.Main.Advanced.ConsoleTitle))
            {
                InternalConfig.Username = "New Window";
                Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);
            }

            // Check for updates
            UpgradeHelper.CheckUpdate();

            // Load command-line arguments
            if (args.Length >= 1)
            {
                try
                {
                    Settings.LoadArguments(args);
                }
                catch (ArgumentException e)
                {
                    InternalConfig.InteractiveMode = false;
                    HandleFailure(e.Message);
                    return;
                }
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

            //Load cached sessions from disk if necessary
            if (Config.Main.Advanced.SessionCache == CacheType.disk)
            {
                bool cacheLoaded = SessionCache.InitializeDiskCache();
                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8" + (cacheLoaded ? Translations.debug_session_cache_ok : Translations.debug_session_cache_fail), acceptnewlines: true);
            }

            // Setup exit cleaning code
            ExitCleanUp.Add(() => { DoExit(0); });

            //Asking the user to type in missing data such as Username and Password
            bool useBrowser = Config.Main.General.AccountType == LoginType.microsoft && Config.Main.General.Method == LoginMethod.browser;
            if (string.IsNullOrWhiteSpace(InternalConfig.Account.Login) && !useBrowser)
            {
                ConsoleIO.WriteLine(ConsoleIO.BasicIO ? Translations.mcc_login_basic_io : Translations.mcc_login);
                InternalConfig.Account.Login = ConsoleIO.ReadLine().Trim();
                if (string.IsNullOrWhiteSpace(InternalConfig.Account.Login))
                {
                    HandleFailure(Translations.error_login_blocked, false, ChatBot.DisconnectReason.LoginRejected);
                    return;
                }
            }
            InternalConfig.Username = InternalConfig.Account.Login;
            if (string.IsNullOrWhiteSpace(InternalConfig.Account.Password) && !useBrowser &&
                (Config.Main.Advanced.SessionCache == CacheType.none || !SessionCache.Contains(ToLowerIfNeed(InternalConfig.Account.Login))))
            {
                RequestPassword();
            }

            startupargs = args;
            InitializeClient();
        }

        /// <summary>
        /// Reduest user to submit password.
        /// </summary>
        private static void RequestPassword()
        {
            ConsoleIO.WriteLine(ConsoleIO.BasicIO ? string.Format(Translations.mcc_password_basic_io, InternalConfig.Account.Login) + "\n" : Translations.mcc_password_hidden);
            string? password = ConsoleIO.BasicIO ? Console.ReadLine() : ConsoleIO.ReadPassword();
            if (string.IsNullOrWhiteSpace(password))
                InternalConfig.Account.Password = "-";
            else
                InternalConfig.Account.Password = password;
        }

        /// <summary>
        /// Start a new Client
        /// </summary>
        private static void InitializeClient()
        {
            InternalConfig.MinecraftVersion = Config.Main.Advanced.MinecraftVersion;

            SessionToken session = new();
            PlayerKeyPair? playerKeyPair = null;

            ProtocolHandler.LoginResult result = ProtocolHandler.LoginResult.LoginRequired;

            string loginLower = ToLowerIfNeed(InternalConfig.Account.Login);
            if (InternalConfig.Account.Password == "-")
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_offline, acceptnewlines: true);
                result = ProtocolHandler.LoginResult.Success;
                session.PlayerID = "0";
                session.PlayerName = InternalConfig.Username;
            }
            else
            {
                // Validate cached session or login new session.
                if (Config.Main.Advanced.SessionCache != CacheType.none && SessionCache.Contains(loginLower))
                {
                    session = SessionCache.Get(loginLower);
                    result = ProtocolHandler.GetTokenValidation(session);
                    if (result != ProtocolHandler.LoginResult.Success)
                    {
                        ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_session_invalid, acceptnewlines: true);
                        // Try to refresh access token
                        if (!string.IsNullOrWhiteSpace(session.RefreshToken))
                        {
                            try
                            {
                                result = ProtocolHandler.MicrosoftLoginRefresh(session.RefreshToken, out session);
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
                            RequestPassword();
                    }
                    else ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_session_valid, session.PlayerName));
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    ConsoleIO.WriteLine(string.Format(Translations.mcc_connecting, Config.Main.General.AccountType == LoginType.mojang ? "Minecraft.net" : "Microsoft"));
                    result = ProtocolHandler.GetLogin(InternalConfig.Account.Login, InternalConfig.Account.Password, Config.Main.General.AccountType, out session);
                }

                if (result == ProtocolHandler.LoginResult.Success && Config.Main.Advanced.SessionCache != CacheType.none)
                    SessionCache.Store(loginLower, session);

                if (result == ProtocolHandler.LoginResult.Success)
                    session.SessionPreCheckTask = Task.Factory.StartNew(() => session.SessionPreCheck());
            }

            if (result == ProtocolHandler.LoginResult.Success)
            {
                InternalConfig.Username = session.PlayerName;
                bool isRealms = false;

                if (OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(Config.Main.Advanced.ConsoleTitle))
                    Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);

                if (Config.Main.Advanced.PlayerHeadAsIcon && OperatingSystem.IsWindows())
                    ConsoleIcon.SetPlayerIconAsync(InternalConfig.Username);

                if (Config.Logging.DebugMessages)
                    ConsoleIO.WriteLine(string.Format(Translations.debug_session_id, session.ID));

                List<string> availableWorlds = new();
                if (Config.Main.Advanced.MinecraftRealms && !String.IsNullOrEmpty(session.ID))
                    availableWorlds = ProtocolHandler.RealmsListWorlds(InternalConfig.Username, session.PlayerID, session.ID);

                if (InternalConfig.ServerIP == string.Empty)
                {
                    ConsoleIO.WriteLine(Translations.mcc_ip);
                    string addressInput = ConsoleIO.ReadLine();
                    if (addressInput.StartsWith("realms:"))
                    {
                        if (Config.Main.Advanced.MinecraftRealms)
                        {
                            if (availableWorlds.Count == 0)
                            {
                                HandleFailure(Translations.error_realms_access_denied, false, ChatBot.DisconnectReason.LoginRejected);
                                return;
                            }
                            string worldId = addressInput.Split(':')[1];
                            if (!availableWorlds.Contains(worldId) && int.TryParse(worldId, NumberStyles.Any, CultureInfo.CurrentCulture, out int worldIndex) && worldIndex < availableWorlds.Count)
                                worldId = availableWorlds[worldIndex];
                            if (availableWorlds.Contains(worldId))
                            {
                                string RealmsAddress = ProtocolHandler.GetRealmsWorldServerAddress(worldId, InternalConfig.Username, session.PlayerID, session.ID);
                                if (RealmsAddress != "")
                                {
                                    addressInput = RealmsAddress;
                                    isRealms = true;
                                    InternalConfig.MinecraftVersion = MCHighestVersion;
                                }
                                else
                                {
                                    HandleFailure(Translations.error_realms_server_unavailable, false, ChatBot.DisconnectReason.LoginRejected);
                                    return;
                                }
                            }
                            else
                            {
                                HandleFailure(Translations.error_realms_server_id, false, ChatBot.DisconnectReason.LoginRejected);
                                return;
                            }
                        }
                        else
                        {
                            HandleFailure(Translations.error_realms_disabled, false, null);
                            return;
                        }
                    }
                    Config.Main.SetServerIP(new MainConfigHealper.MainConfig.ServerInfoConfig(addressInput), true);
                }

                //Get server version
                int protocolversion = 0;
                ForgeInfo? forgeInfo = null;

                if (InternalConfig.MinecraftVersion != "" && Settings.ToLowerIfNeed(InternalConfig.MinecraftVersion) != "auto")
                {
                    protocolversion = Protocol.ProtocolHandler.MCVer2ProtocolVersion(InternalConfig.MinecraftVersion);

                    if (protocolversion != 0)
                        ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_use_version, InternalConfig.MinecraftVersion, protocolversion));
                    else
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_unknown_version, InternalConfig.MinecraftVersion));

                    if (useMcVersionOnce)
                    {
                        useMcVersionOnce = false;
                        InternalConfig.MinecraftVersion = "";
                    }
                }

                //Retrieve server info if version is not manually set OR if need to retrieve Forge information
                if (!isRealms && (protocolversion == 0 || (Config.Main.Advanced.EnableForge == ForgeConfigType.auto) ||
                    ((Config.Main.Advanced.EnableForge == ForgeConfigType.force) && !ProtocolHandler.ProtocolMayForceForge(protocolversion))))
                {
                    if (protocolversion != 0)
                        ConsoleIO.WriteLine(Translations.mcc_forge);
                    else
                        ConsoleIO.WriteLine(Translations.mcc_retrieve);
                    if (!ProtocolHandler.GetServerInfo(InternalConfig.ServerIP, InternalConfig.ServerPort, ref protocolversion, ref forgeInfo))
                    {
                        HandleFailure(Translations.error_ping, true, ChatBots.AutoRelog.DisconnectReason.ConnectionLost);
                        return;
                    }
                }

                if (Config.Main.General.AccountType == LoginType.microsoft
                    && (InternalConfig.Account.Password != "-" || Config.Main.General.Method == LoginMethod.browser)
                    && Config.Signature.LoginWithSecureProfile
                    && protocolversion >= 759 /* 1.19 and above */)
                {
                    // Load cached profile key from disk if necessary
                    if (Config.Main.Advanced.ProfileKeyCache == CacheType.disk)
                    {
                        bool cacheKeyLoaded = KeysCache.InitializeDiskCache();
                        if (Config.Logging.DebugMessages)
                            ConsoleIO.WriteLineFormatted("§8" + (cacheKeyLoaded ? Translations.debug_keys_cache_ok : Translations.debug_keys_cache_fail), acceptnewlines: true);
                    }

                    if (Config.Main.Advanced.ProfileKeyCache != CacheType.none && KeysCache.Contains(loginLower))
                    {
                        playerKeyPair = KeysCache.Get(loginLower);
                        if (playerKeyPair.NeedRefresh())
                            ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_profile_key_invalid, acceptnewlines: true);
                        else
                            ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_profile_key_valid, session.PlayerName));
                    }

                    if (playerKeyPair == null || playerKeyPair.NeedRefresh())
                    {
                        ConsoleIO.WriteLineFormatted(Translations.mcc_fetching_key, acceptnewlines: true);
                        playerKeyPair = KeyUtils.GetNewProfileKeys(session.ID);
                        if (Config.Main.Advanced.ProfileKeyCache != CacheType.none && playerKeyPair != null)
                        {
                            KeysCache.Store(loginLower, playerKeyPair);
                        }
                    }
                }

                //Force-enable Forge support?
                if (!isRealms && (Config.Main.Advanced.EnableForge == ForgeConfigType.force) && forgeInfo == null)
                {
                    if (ProtocolHandler.ProtocolMayForceForge(protocolversion))
                    {
                        ConsoleIO.WriteLine(Translations.mcc_forgeforce);
                        forgeInfo = ProtocolHandler.ProtocolForceForge(protocolversion);
                    }
                    else
                    {
                        HandleFailure(Translations.error_forgeforce, true, ChatBots.AutoRelog.DisconnectReason.ConnectionLost);
                        return;
                    }
                }

                //Proceed to server login
                if (protocolversion != 0)
                {
                    try
                    {
                        //Start the main TCP client
                        client = new McClient(session, playerKeyPair, InternalConfig.ServerIP, InternalConfig.ServerPort, protocolversion, forgeInfo);

                        //Update console title
                        if (OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(Config.Main.Advanced.ConsoleTitle))
                            Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);
                    }
                    catch (NotSupportedException)
                    {
                        HandleFailure(Translations.error_unsupported, true);
                    }
                    catch (NotImplementedException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        ConsoleIO.WriteLine(e.Message);
                        ConsoleIO.WriteLine(e.StackTrace ?? "");
                        HandleFailure(); // Other error
                    }
                }
                else HandleFailure(Translations.error_determine, true);
            }
            else
            {
                string failureMessage = Translations.error_login;
                string failureReason = string.Empty;
                failureReason = result switch
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
                failureMessage += failureReason;
                HandleFailure(failureMessage, false, ChatBot.DisconnectReason.LoginRejected);
            }
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
        public static void WriteBackSettings(bool enableBackup = true)
        {
            Settings.WriteToFile(settingsIniPath, enableBackup);
        }

        /// <summary>
        /// Disconnect the current client from the server and restart it
        /// </summary>
        /// <param name="delaySeconds">Optional delay, in seconds, before restarting</param>
        public static void Restart(int delaySeconds = 0, bool keepAccountAndServerSettings = false)
        {
            ConsoleInteractive.ConsoleReader.StopReadThread();
            new Thread(new ThreadStart(delegate
            {
                if (client != null) { client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Item2.Cancel(); offlinePrompt.Item1.Join(); offlinePrompt = null; ConsoleIO.Reset(); }
                if (delaySeconds > 0)
                {
                    ConsoleIO.WriteLine(string.Format(Translations.mcc_restart_delay, delaySeconds));
                    Thread.Sleep(delaySeconds * 1000);
                }
                ConsoleIO.WriteLine(Translations.mcc_restart);
                ReloadSettings(keepAccountAndServerSettings);
                InitializeClient();
            })).Start();
        }

        public static void DoExit(int exitcode = 0)
        {
            WriteBackSettings(true);
            ConsoleInteractive.ConsoleSuggestion.ClearSuggestions();
            ConsoleIO.WriteLineFormatted("§a" + string.Format(Translations.config_saving, settingsIniPath));

            if (client != null) { client.Disconnect(); ConsoleIO.Reset(); }
            if (offlinePrompt != null) { offlinePrompt.Item2.Cancel(); offlinePrompt.Item1.Join(); offlinePrompt = null; ConsoleIO.Reset(); }
            if (Config.Main.Advanced.PlayerHeadAsIcon) { ConsoleIcon.RevertToMCCIcon(); }
            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>
        public static void Exit(int exitcode = 0)
        {
            new Thread(new ThreadStart(() => { DoExit(exitcode); })).Start();
        }

        /// <summary>
        /// Handle fatal errors such as ping failure, login failure, server disconnection, and so on.
        /// Allows AutoRelog to perform on fatal errors, prompt for server version, and offline commands.
        /// </summary>
        /// <param name="errorMessage">Error message to display and optionally pass to AutoRelog bot</param>
        /// <param name="versionError">Specify if the error is related to an incompatible or unkown server version</param>
        /// <param name="disconnectReason">If set, the error message will be processed by the AutoRelog bot</param>
        public static void HandleFailure(string? errorMessage = null, bool versionError = false, ChatBots.AutoRelog.DisconnectReason? disconnectReason = null)
        {
            if (!String.IsNullOrEmpty(errorMessage))
            {
                ConsoleIO.Reset();
                while (Console.KeyAvailable)
                    Console.ReadKey(true);
                ConsoleIO.WriteLine(errorMessage);

                if (disconnectReason.HasValue)
                {
                    if (ChatBots.AutoRelog.OnDisconnectStatic(disconnectReason.Value, errorMessage))
                        return; //AutoRelog is triggering a restart of the client
                }
            }

            if (InternalConfig.InteractiveMode)
            {
                if (versionError)
                {
                    ConsoleIO.WriteLine(Translations.mcc_server_version);
                    InternalConfig.MinecraftVersion = ConsoleInteractive.ConsoleReader.RequestImmediateInput();
                    if (InternalConfig.MinecraftVersion != "")
                    {
                        useMcVersionOnce = true;
                        Restart();
                        return;
                    }
                }

                if (offlinePrompt == null)
                {
                    ConsoleInteractive.ConsoleReader.StopReadThread();

                    var cancellationTokenSource = new CancellationTokenSource();
                    offlinePrompt = new(new Thread(new ThreadStart(delegate
                    {
                        bool exitThread = false;
                        string command = " ";
                        ConsoleIO.WriteLine(string.Empty);
                        ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_disconnected, Config.Main.Advanced.InternalCmdChar.ToLogString()));
                        ConsoleIO.WriteLineFormatted(Translations.mcc_press_exit, acceptnewlines: true);

                        while (!cancellationTokenSource.IsCancellationRequested)
                        {
                            if (exitThread)
                                return;

                            while (command.Length > 0)
                            {
                                if (cancellationTokenSource.IsCancellationRequested)
                                    return;

                                command = ConsoleInteractive.ConsoleReader.RequestImmediateInput().Trim();
                                if (command.Length > 0)
                                {
                                    string message = "";

                                    if (Config.Main.Advanced.InternalCmdChar.ToChar() != ' '
                                        && command[0] == Config.Main.Advanced.InternalCmdChar.ToChar())
                                        command = command[1..];

                                    if (command.StartsWith("reco"))
                                    {
                                        message = Commands.Reco.DoReconnect(Config.AppVar.ExpandVars(command));
                                        if (message == "")
                                        {
                                            exitThread = true;
                                            break;
                                        }
                                    }
                                    else if (command.StartsWith("connect"))
                                    {
                                        message = Commands.Connect.DoConnect(Config.AppVar.ExpandVars(command));
                                        if (message == "")
                                        {
                                            exitThread = true;
                                            break;
                                        }
                                    }
                                    else if (command.StartsWith("exit") || command.StartsWith("quit"))
                                    {
                                        message = Commands.Exit.DoExit(Config.AppVar.ExpandVars(command));
                                    }
                                    else if (command.StartsWith("help"))
                                    {
                                        ConsoleIO.WriteLineFormatted("§8MCC: " +
                                                                     Config.Main.Advanced.InternalCmdChar.ToLogString() +
                                                                     new Commands.Reco().GetCmdDescTranslated());
                                        ConsoleIO.WriteLineFormatted("§8MCC: " +
                                                                     Config.Main.Advanced.InternalCmdChar.ToLogString() +
                                                                     new Commands.Connect().GetCmdDescTranslated());
                                    }
                                    else
                                        ConsoleIO.WriteLineFormatted(string.Format(Translations.icmd_unknown, command.Split(' ')[0]));

                                    if (message != "")
                                        ConsoleIO.WriteLineFormatted("§8MCC: " + message);
                                }
                                else
                                {
                                    Commands.Exit.DoExit(Config.AppVar.ExpandVars(command));
                                }
                            }

                            if (exitThread)
                                return;
                        }
                    })), cancellationTokenSource);
                    offlinePrompt.Item1.Start();
                }
            }
            else
            {
                // Not in interactive mode, just exit and let the calling script handle the failure
                if (disconnectReason.HasValue)
                {
                    // Return distinct exit codes for known failures.
                    if (disconnectReason.Value == ChatBot.DisconnectReason.UserLogout) Exit(1);
                    if (disconnectReason.Value == ChatBot.DisconnectReason.InGameKick) Exit(2);
                    if (disconnectReason.Value == ChatBot.DisconnectReason.ConnectionLost) Exit(3);
                    if (disconnectReason.Value == ChatBot.DisconnectReason.LoginRejected) Exit(4);
                }
                Exit();
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
            return assembly.GetTypes().Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

        /// <summary>
        /// Static initialization of build information, read from assembly information
        /// </summary>
        static Program()
        {
            if (typeof(Program)
                .Assembly
                .GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false)
                .FirstOrDefault() is AssemblyConfigurationAttribute attribute)
                BuildInfo = attribute.Configuration;
        }
    }
}
