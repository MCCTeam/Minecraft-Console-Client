using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Keys;
using MinecraftClient.Protocol.Session;
using MinecraftClient.WinAPI;
using Tomlet;
using static MinecraftClient.Settings;
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
        public static readonly CultureInfo ActualCulture = CultureInfo.CurrentCulture;

        public const string Version = MCHighestVersion;
        public const string MCLowestVersion = "1.4.6";
        public const string MCHighestVersion = "1.19.2";
        public static readonly string? BuildInfo = null;

        private static Tuple<Thread, CancellationTokenSource>? offlinePrompt = null;
        private static bool useMcVersionOnce = false;
        private static string settingsIniPath = "MinecraftClient.ini";

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>
        static void Main(string[] args)
        {
            InitCulture();

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
            {
                ConsoleIO.WriteLineFormatted("§8" + BuildInfo);
            }

            //Debug input ?
            if (args.Length == 1 && args[0] == "--keyboard-debug")
            {
                ConsoleIO.WriteLine("Keyboard debug mode: Press any key to display info");
                ConsoleIO.DebugReadInput();
            }

            //Process ini configuration file
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

            if (!loadSucceed)
            {
                ConsoleInteractive.ConsoleReader.StopReadThread();
                string command = " ";
                while (command.Length > 0)
                {
                    ConsoleIO.WriteLine(string.Empty);
                    ConsoleIO.WriteLineFormatted(Translations.Get("mcc.invaild_config", Config.Main.Advanced.InternalCmdChar.ToLogString()));
                    Translations.WriteLineFormatted("mcc.press_exit");
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
                            (string gameLanguage, string[] langList) = Translations.GetTranslationPriority();
                            Config.Main.Advanced.Language = gameLanguage;
                            Translations.LoadTranslationFile(langList);

                            WriteBackSettings(true);
                            ConsoleIO.WriteLineFormatted(Translations.Get("mcc.gen_new_config", settingsIniPath));
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
            else if (needWriteDefaultSetting)
            {
                (string gameLanguage, string[] langList) = Translations.GetTranslationPriority();
                Translations.LoadTranslationFile(langList);
                Config.Main.Advanced.Language = gameLanguage;

                WriteBackSettings(false);
                if (newlyGenerated)
                    ConsoleIO.WriteLineFormatted(Translations.TryGet("mcc.settings_generated"));
                ConsoleIO.WriteLine(Translations.TryGet("mcc.run_with_default_settings"));
            }
            else
            {
                //Load external translation file. Should be called AFTER settings loaded
                Translations.LoadTranslationFile(Translations.GetTranslationPriority(Config.Main.Advanced.Language));
                if (!Config.Main.Advanced.Language.StartsWith("en"))
                    ConsoleIO.WriteLine(Translations.TryGet("mcc.help_us_translate", Settings.TranslationDocUrl));
                WriteBackSettings(true); // format
            }

            bool needPromptUpdate = true;
            if (Settings.CheckUpdate(Config.Head.CurrentVersion, Config.Head.LatestVersion))
            {
                needPromptUpdate = false;
                ConsoleIO.WriteLineFormatted(Translations.TryGet("mcc.has_update", Settings.GithubLatestReleaseUrl));
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

                if (args.Contains("--trim-translation"))
                {
                    Translations.TrimAllTranslations();
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
                                throw new ArgumentException(Translations.Get("error.setting.argument_syntax", argument));

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
                        Console.WriteLine(Translations.Get("error.generator.invalid"));
                        Console.WriteLine(Translations.Get("error.usage") + " MinecraftClient.exe --data-generator=<entity|item|block> --data-path=\"<path to resources.json>\"");
                        return;
                    }

                    if (string.IsNullOrEmpty(dataPath))
                    {
                        Console.WriteLine(Translations.Get("error.missing.argument", "--data-path"));
                        Console.WriteLine(Translations.Get("error.usage") + " MinecraftClient.exe --data-generator=<entity|item|block> --data-path=\"<path to resources.json>\"");
                        return;
                    }

                    if (!File.Exists(dataPath))
                    {
                        Console.WriteLine(Translations.Get("error.generator.path", dataPath));
                        return;
                    }

                    if (!dataPath.EndsWith(".json"))
                    {
                        Console.WriteLine(Translations.Get("error.generator.json", dataPath));
                        return;
                    }

                    Console.WriteLine(Translations.Get("mcc.generator.generating", dataGenerator, dataPath));

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

                    Console.WriteLine(Translations.Get("mcc.generator.done", dataGenerator, dataPath));
                    return;
                }

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

            // Check for updates
            Task.Run(() =>
            {
                HttpClientHandler httpClientHandler = new() { AllowAutoRedirect = false };
                HttpClient httpClient = new(httpClientHandler);
                Task<HttpResponseMessage>? httpWebRequest = null;
                try
                {
                    httpWebRequest = httpClient.GetAsync(Settings.GithubLatestReleaseUrl, HttpCompletionOption.ResponseHeadersRead);
                    httpWebRequest.Wait();
                    HttpResponseMessage res = httpWebRequest.Result;
                    if (res.Headers.Location != null)
                    {
                        Match match = Regex.Match(res.Headers.Location.ToString(), Settings.GithubReleaseUrl + @"/tag/(\d{4})(\d{2})(\d{2})-(\d+)");
                        if (match.Success && match.Groups.Count == 5)
                        {
                            string year = match.Groups[1].Value, month = match.Groups[2].Value, day = match.Groups[3].Value, run = match.Groups[4].Value;
                            string latestVersion = string.Format("GitHub build {0}, built on {1}-{2}-{3}", run, year, month, day);
                            if (needPromptUpdate)
                                if (Settings.CheckUpdate(Config.Head.CurrentVersion, Config.Head.LatestVersion))
                                    ConsoleIO.WriteLineFormatted(Translations.TryGet("mcc.has_update", Settings.GithubLatestReleaseUrl));
                            if (latestVersion != Config.Head.LatestVersion)
                            {
                                Config.Head.LatestVersion = latestVersion;
                                WriteBackSettings(false);
                            }
                        }
                    }
                }
                catch (Exception) { }
                finally { httpWebRequest?.Dispose(); }
                httpClient.Dispose();
                httpClientHandler.Dispose();
            });

            if (Config.Main.Advanced.ConsoleTitle != "")
            {
                InternalConfig.Username = "New Window";
                Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);
            }

            //Test line to troubleshoot invisible colors
            if (Config.Logging.DebugMessages)
            {
                ConsoleIO.WriteLineFormatted(Translations.Get("debug.color_test", "[0123456789ABCDEF]: [§00§11§22§33§44§55§66§77§88§99§aA§bB§cC§dD§eE§fF§r]"));
            }

            //Load cached sessions from disk if necessary
            if (Config.Main.Advanced.SessionCache == CacheType.disk)
            {
                bool cacheLoaded = SessionCache.InitializeDiskCache();
                if (Config.Logging.DebugMessages)
                    Translations.WriteLineFormatted(cacheLoaded ? "debug.session_cache_ok" : "debug.session_cache_fail");
            }

            // Setup exit cleaning code
            ExitCleanUp.Add(() => { DoExit(0); });

            //Asking the user to type in missing data such as Username and Password
            bool useBrowser = Config.Main.General.AccountType == LoginType.microsoft && Config.Main.General.Method == LoginMethod.browser;
            if (string.IsNullOrEmpty(Config.Main.General.Account.Login) && !useBrowser)
            {
                ConsoleIO.WriteLine(ConsoleIO.BasicIO ? Translations.Get("mcc.login_basic_io") : Translations.Get("mcc.login"));
                Config.Main.General.Account.Login = ConsoleIO.ReadLine().Trim();
                if (string.IsNullOrEmpty(Config.Main.General.Account.Login))
                {
                    HandleFailure(Translations.Get("error.login.blocked"), false, ChatBot.DisconnectReason.LoginRejected);
                    return;
                }
            }
            InternalConfig.Username = Config.Main.General.Account.Login;
            if (string.IsNullOrEmpty(Config.Main.General.Account.Password) && !useBrowser &&
                (Config.Main.Advanced.SessionCache == CacheType.none || !SessionCache.Contains(Settings.ToLowerIfNeed(Config.Main.General.Account.Login))))
            {
                RequestPassword();
            }
            else
            {
                InternalConfig.Password = Config.Main.General.Account.Password;
            }

            startupargs = args;
            InitializeClient();
        }

        /// <summary>
        /// Reduest user to submit password.
        /// </summary>
        private static void RequestPassword()
        {
            ConsoleIO.WriteLine(ConsoleIO.BasicIO ? Translations.Get("mcc.password_basic_io", Config.Main.General.Account.Login) + "\n" : Translations.Get("mcc.password"));
            string? password = ConsoleIO.BasicIO ? Console.ReadLine() : ConsoleIO.ReadPassword();
            if (password == null || password == string.Empty) { password = "-"; }
            InternalConfig.Password = password;
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

            string loginLower = Settings.ToLowerIfNeed(Config.Main.General.Account.Login);
            if (InternalConfig.Password == "-")
            {
                Translations.WriteLineFormatted("mcc.offline");
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
                        Translations.WriteLineFormatted("mcc.session_invalid");
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
                            && InternalConfig.Password == ""
                            && Config.Main.General.AccountType == LoginType.mojang)
                            RequestPassword();
                    }
                    else ConsoleIO.WriteLineFormatted(Translations.Get("mcc.session_valid", session.PlayerName));
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    Translations.WriteLine("mcc.connecting", Config.Main.General.AccountType == LoginType.mojang ? "Minecraft.net" : "Microsoft");
                    result = ProtocolHandler.GetLogin(Config.Main.General.Account.Login, InternalConfig.Password, Config.Main.General.AccountType, out session);
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

                if (Config.Main.Advanced.ConsoleTitle != "")
                    Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);

                if (Config.Main.Advanced.PlayerHeadAsIcon && OperatingSystem.IsWindows())
                    ConsoleIcon.SetPlayerIconAsync(InternalConfig.Username);

                if (Config.Logging.DebugMessages)
                    Translations.WriteLine("debug.session_id", session.ID);

                List<string> availableWorlds = new();
                if (Config.Main.Advanced.MinecraftRealms && !String.IsNullOrEmpty(session.ID))
                    availableWorlds = ProtocolHandler.RealmsListWorlds(InternalConfig.Username, session.PlayerID, session.ID);

                if (InternalConfig.ServerIP == string.Empty)
                {
                    Translations.Write("mcc.ip");
                    string addressInput = ConsoleIO.ReadLine();
                    if (addressInput.StartsWith("realms:"))
                    {
                        if (Config.Main.Advanced.MinecraftRealms)
                        {
                            if (availableWorlds.Count == 0)
                            {
                                HandleFailure(Translations.Get("error.realms.access_denied"), false, ChatBot.DisconnectReason.LoginRejected);
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
                                    HandleFailure(Translations.Get("error.realms.server_unavailable"), false, ChatBot.DisconnectReason.LoginRejected);
                                    return;
                                }
                            }
                            else
                            {
                                HandleFailure(Translations.Get("error.realms.server_id"), false, ChatBot.DisconnectReason.LoginRejected);
                                return;
                            }
                        }
                        else
                        {
                            HandleFailure(Translations.Get("error.realms.disabled"), false, null);
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
                        ConsoleIO.WriteLineFormatted(Translations.Get("mcc.use_version", InternalConfig.MinecraftVersion, protocolversion));
                    else
                        ConsoleIO.WriteLineFormatted(Translations.Get("mcc.unknown_version", InternalConfig.MinecraftVersion));

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
                        Translations.WriteLine("mcc.forge");
                    else Translations.WriteLine("mcc.retrieve");
                    if (!ProtocolHandler.GetServerInfo(InternalConfig.ServerIP, InternalConfig.ServerPort, ref protocolversion, ref forgeInfo))
                    {
                        HandleFailure(Translations.Get("error.ping"), true, ChatBots.AutoRelog.DisconnectReason.ConnectionLost);
                        return;
                    }
                }

                if (Config.Main.General.AccountType == LoginType.microsoft && InternalConfig.Password != "-" && 
                    Config.Signature.LoginWithSecureProfile && protocolversion >= 759 /* 1.19 and above */)
                {
                    // Load cached profile key from disk if necessary
                    if (Config.Main.Advanced.ProfileKeyCache == CacheType.disk)
                    {
                        bool cacheKeyLoaded = KeysCache.InitializeDiskCache();
                        if (Config.Logging.DebugMessages)
                            Translations.WriteLineFormatted(cacheKeyLoaded ? "debug.keys_cache_ok" : "debug.keys_cache_fail");
                    }

                    if (Config.Main.Advanced.ProfileKeyCache != CacheType.none && KeysCache.Contains(loginLower))
                    {
                        playerKeyPair = KeysCache.Get(loginLower);
                        if (playerKeyPair.NeedRefresh())
                            Translations.WriteLineFormatted("mcc.profile_key_invalid");
                        else
                            ConsoleIO.WriteLineFormatted(Translations.Get("mcc.profile_key_valid", session.PlayerName));
                    }

                    if (playerKeyPair == null || playerKeyPair.NeedRefresh())
                    {
                        Translations.WriteLineFormatted("mcc.fetching_key");
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
                        Translations.WriteLine("mcc.forgeforce");
                        forgeInfo = ProtocolHandler.ProtocolForceForge(protocolversion);
                    }
                    else
                    {
                        HandleFailure(Translations.Get("error.forgeforce"), true, ChatBots.AutoRelog.DisconnectReason.ConnectionLost);
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
                        if (Config.Main.Advanced.ConsoleTitle != "")
                            Console.Title = Config.AppVar.ExpandVars(Config.Main.Advanced.ConsoleTitle);
                    }
                    catch (NotSupportedException)
                    {
                        HandleFailure(Translations.Get("error.unsupported"), true);
                    }
                    catch (Exception) { }
                }
                else HandleFailure(Translations.Get("error.determine"), true);
            }
            else
            {
                string failureMessage = Translations.Get("error.login");
                string failureReason = "";
                failureReason = result switch
                {
#pragma warning disable format // @formatter:off
                    ProtocolHandler.LoginResult.AccountMigrated     =>  "error.login.migrated",
                    ProtocolHandler.LoginResult.ServiceUnavailable  =>  "error.login.server",
                    ProtocolHandler.LoginResult.WrongPassword       =>  "error.login.blocked",
                    ProtocolHandler.LoginResult.InvalidResponse     =>  "error.login.response",
                    ProtocolHandler.LoginResult.NotPremium          =>  "error.login.premium",
                    ProtocolHandler.LoginResult.OtherError          =>  "error.login.network",
                    ProtocolHandler.LoginResult.SSLError            =>  "error.login.ssl",
                    ProtocolHandler.LoginResult.UserCancel          =>  "error.login.cancel",
                    _                                               =>  "error.login.unknown",
#pragma warning restore format // @formatter:on
                };
                failureMessage += Translations.Get(failureReason);
                HandleFailure(failureMessage, false, ChatBot.DisconnectReason.LoginRejected);
            }
        }

        /// <summary>
        /// Reloads settings
        /// </summary>
        public static void ReloadSettings()
        {
            if(Settings.LoadFromFile(settingsIniPath).Item1)
                ConsoleIO.WriteLine(Translations.TryGet("config.loading", settingsIniPath));
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
        public static void Restart(int delaySeconds = 0)
        {
            ConsoleInteractive.ConsoleReader.StopReadThread();
            new Thread(new ThreadStart(delegate
            {
                if (client != null) { client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Item2.Cancel(); offlinePrompt.Item1.Join(); offlinePrompt = null; ConsoleIO.Reset(); }
                if (delaySeconds > 0)
                {
                    Translations.WriteLine("mcc.restart_delay", delaySeconds);
                    System.Threading.Thread.Sleep(delaySeconds * 1000);
                }
                Translations.WriteLine("mcc.restart");
                ReloadSettings();
                InitializeClient();
            })).Start();
        }

        public static void DoExit(int exitcode = 0)
        {
            WriteBackSettings(true);
            ConsoleIO.WriteLineFormatted(Translations.TryGet("config.saving", settingsIniPath));

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
                    Translations.Write("mcc.server_version");
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
                        ConsoleIO.WriteLineFormatted(Translations.Get("mcc.disconnected", Config.Main.Advanced.InternalCmdChar.ToLogString()));
                        Translations.WriteLineFormatted("mcc.press_exit");

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
                                        message = new Commands.Reco().Run(null, Config.AppVar.ExpandVars(command), null);
                                        if (message == "")
                                        {
                                            exitThread = true;
                                            break;
                                        }
                                    }
                                    else if (command.StartsWith("connect"))
                                    {
                                        message = new Commands.Connect().Run(null, Config.AppVar.ExpandVars(command), null);
                                        if (message == "")
                                        {
                                            exitThread = true;
                                            break;
                                        }
                                    }
                                    else if (command.StartsWith("exit") || command.StartsWith("quit"))
                                    {
                                        message = new Commands.Exit().Run(null, Config.AppVar.ExpandVars(command), null);
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
                                        ConsoleIO.WriteLineFormatted(Translations.Get("icmd.unknown", command.Split(' ')[0]));

                                    if (message != "")
                                        ConsoleIO.WriteLineFormatted("§8MCC: " + message);
                                }
                                else
                                {
                                    _ = new Commands.Exit().Run(null, Config.AppVar.ExpandVars(command), null);
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
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

        public static void InitCulture()
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
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
    }
}
