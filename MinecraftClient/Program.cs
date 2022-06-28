using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol;
using System.Reflection;
using System.Threading;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.WinAPI;

namespace MinecraftClient
{
    /// <summary>
    /// Minecraft Console Client by the MCC Team (c) 2012-2021.
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
        private static McClient client;
        public static string[] startupargs;

        public const string Version = MCHighestVersion;
        public const string MCLowestVersion = "1.4.6";
        public const string MCHighestVersion = "1.18.2";
        public static readonly string BuildInfo = null;

        private static Thread offlinePrompt = null;
        private static bool useMcVersionOnce = false;

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>
        static void Main(string[] args)
        {
            Console.WriteLine("Minecraft Console Client v{0} - for MC {1} to {2} - Github.com/MCCTeam", Version, MCLowestVersion, MCHighestVersion);

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

            //Setup ConsoleIO
            ConsoleIO.LogPrefix = "§8[MCC] ";
            if (args.Length >= 1 && args[args.Length - 1] == "BasicIO" || args.Length >= 1 && args[args.Length - 1] == "BasicIO-NoColor")
            {
                if (args.Length >= 1 && args[args.Length - 1] == "BasicIO-NoColor")
                {
                    ConsoleIO.BasicIO_NoColor = true;
                }
                ConsoleIO.BasicIO = true;
                args = args.Where(o => !Object.ReferenceEquals(o, args[args.Length - 1])).ToArray();
            }

            //Take advantage of Windows 10 / Mac / Linux UTF-8 console
            if (isUsingMono || WindowsVersion.WinMajorVersion >= 10)
            {
                Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
            }

            //Process ini configuration file
            if (args.Length >= 1 && System.IO.File.Exists(args[0]) && System.IO.Path.GetExtension(args[0]).ToLower() == ".ini")
            {
                Settings.LoadFile(args[0]);

                //remove ini configuration file from arguments array
                List<string> args_tmp = args.ToList<string>();
                args_tmp.RemoveAt(0);
                args = args_tmp.ToArray();
            }
            else if (System.IO.File.Exists("MinecraftClient.ini"))
            {
                Settings.LoadFile("MinecraftClient.ini");
            }
            else Settings.WriteDefaultSettings("MinecraftClient.ini");

            //Load external translation file. Should be called AFTER settings loaded
            Translations.LoadExternalTranslationFile(Settings.Language);

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

                if (args.Contains("--generate"))
                {
                    string dataGenerator = "";
                    string dataPath = "";

                    foreach (string argument in args)
                    {
                        if (argument.StartsWith("--") && !argument.Contains("--generate"))
                        {
                            if (!argument.Contains("="))
                                throw new ArgumentException(Translations.Get("error.setting.argument_syntax", argument));
                            
                            string[] argParts = argument.Substring(2).Split('=');
                            string argName = argParts[0].Trim();
                            string argValue = argParts[1].Replace("\"", "").Trim();

                            if(argName == "data-path") {
                                Console.WriteLine(dataPath);
                                dataPath = argValue;
                            }

                            if(argName == "data-generator") {
                                dataGenerator = argValue;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(dataGenerator) || !(dataGenerator.ToLower().Equals("entity") || dataGenerator.ToLower().Equals("item") || dataGenerator.ToLower().Equals("block")))
                    {
                        Console.WriteLine(Translations.Get("error.generator.invalid"));
                        Console.WriteLine(Translations.Get("error.usage") + " MinecraftClient.exe --data-generator=<entity|item|block>");
                        return;
                    }

                    if (string.IsNullOrEmpty(dataPath))
                    {
                        Console.WriteLine(Translations.Get("error.missing.argument", "--data-path"));
                        Console.WriteLine(Translations.Get("error.usage") + " MinecraftClient.exe --generate-entity-pallete --data-path=\"<path to resources.json>\"");
                        return;
                    }

                    if(!File.Exists(dataPath)) 
                    {
                        Console.WriteLine(Translations.Get("error.generator.path", dataPath));
                        return;
                    }

                    if(!dataPath.EndsWith(".json")) 
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
                            BlockPaletteGenerator.GenerateBlockPallete(dataPath);
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
                    Settings.interactiveMode = false;
                    HandleFailure(e.Message);
                    return;
                }
            }

            if (Settings.ConsoleTitle != "")
            {
                Settings.Username = "New Window";
                Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);
            }

            //Test line to troubleshoot invisible colors
            if (Settings.DebugMessages)
            {
                ConsoleIO.WriteLineFormatted(Translations.Get("debug.color_test", "[0123456789ABCDEF]: [§00§11§22§33§44§55§66§77§88§99§aA§bB§cC§dD§eE§fF§r]"));
            }

            //Load cached sessions from disk if necessary
            if (Settings.SessionCaching == CacheType.Disk)
            {
                bool cacheLoaded = SessionCache.InitializeDiskCache();
                if (Settings.DebugMessages)
                    Translations.WriteLineFormatted(cacheLoaded ? "debug.session_cache_ok" : "debug.session_cache_fail");
            }

            //Asking the user to type in missing data such as Username and Password
            bool useBrowser = Settings.AccountType == ProtocolHandler.AccountType.Microsoft && Settings.LoginMethod == "browser";
            if (Settings.Login == "" && !useBrowser)
            {
                Console.Write(ConsoleIO.BasicIO ? Translations.Get("mcc.login_basic_io") + "\n" : Translations.Get("mcc.login"));
                Settings.Login = Console.ReadLine();
            }
            if (Settings.Password == "" 
                && (Settings.SessionCaching == CacheType.None || !SessionCache.Contains(Settings.Login.ToLower()))
                && !useBrowser)
            {
                RequestPassword();
            }

            // Setup exit cleaning code
            ExitCleanUp.Add(delegate () 
            {
                // Do NOT use Program.Exit() as creating new Thread cause program to freeze
                if (client != null) { client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Abort(); offlinePrompt = null; ConsoleIO.Reset(); }
                if (Settings.playerHeadAsIcon) { ConsoleIcon.revertToMCCIcon(); }
            });
            

            startupargs = args;
            InitializeClient();
        }

        /// <summary>
        /// Reduest user to submit password.
        /// </summary>
        private static void RequestPassword()
        {
            Console.Write(ConsoleIO.BasicIO ? Translations.Get("mcc.password_basic_io", Settings.Login) + "\n" : Translations.Get("mcc.password"));
            Settings.Password = ConsoleIO.BasicIO ? Console.ReadLine() : ConsoleIO.ReadPassword();
            if (Settings.Password == "") { Settings.Password = "-"; }
            if (!ConsoleIO.BasicIO)
            {
                //Hide password length
                Console.CursorTop--; Console.Write(Translations.Get("mcc.password_hidden", "<******>"));
                for (int i = 19; i < Console.BufferWidth; i++) { Console.Write(' '); }
            }
        }

        /// <summary>
        /// Start a new Client
        /// </summary>
        private static void InitializeClient()
        {
            SessionToken session = new SessionToken();

            ProtocolHandler.LoginResult result = ProtocolHandler.LoginResult.LoginRequired;

            if (Settings.Password == "-")
            {
                Translations.WriteLineFormatted("mcc.offline");
                result = ProtocolHandler.LoginResult.Success;
                session.PlayerID = "0";
                session.PlayerName = Settings.Login;
            }
            else
            {
                // Validate cached session or login new session.
                if (Settings.SessionCaching != CacheType.None && SessionCache.Contains(Settings.Login.ToLower()))
                {
                    session = SessionCache.Get(Settings.Login.ToLower());
                    result = ProtocolHandler.GetTokenValidation(session);
                    if (result != ProtocolHandler.LoginResult.Success)
                    {
                        Translations.WriteLineFormatted("mcc.session_invalid");
                        // Try to refresh access token
                        if (!string.IsNullOrWhiteSpace(session.RefreshToken))
                        {
                            result = ProtocolHandler.MicrosoftLoginRefresh(session.RefreshToken, out session);
                        }
                        if (result != ProtocolHandler.LoginResult.Success 
                            && Settings.Password == "" 
                            && Settings.AccountType == ProtocolHandler.AccountType.Mojang)
                            RequestPassword();
                    }
                    else ConsoleIO.WriteLineFormatted(Translations.Get("mcc.session_valid", session.PlayerName));
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    Translations.WriteLine("mcc.connecting", Settings.AccountType == ProtocolHandler.AccountType.Mojang ? "Minecraft.net" : "Microsoft");
                    result = ProtocolHandler.GetLogin(Settings.Login, Settings.Password, Settings.AccountType, out session);
                }
            }

            if (result == ProtocolHandler.LoginResult.Success && Settings.SessionCaching != CacheType.None)
            {
                SessionCache.Store(Settings.Login.ToLower(), session);
            }

            if (result == ProtocolHandler.LoginResult.Success)
            {
                Settings.Username = session.PlayerName;
                bool isRealms = false;

                if (Settings.ConsoleTitle != "")
                    Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);

                if (Settings.playerHeadAsIcon)
                    ConsoleIcon.setPlayerIconAsync(Settings.Username);

                if (Settings.DebugMessages)
                    Translations.WriteLine("debug.session_id", session.ID);

                List<string> availableWorlds = new List<string>();
                if (Settings.MinecraftRealmsEnabled && !String.IsNullOrEmpty(session.ID))
                    availableWorlds = ProtocolHandler.RealmsListWorlds(Settings.Username, session.PlayerID, session.ID);

                if (Settings.ServerIP == "")
                {
                    Translations.Write("mcc.ip");
                    string addressInput = Console.ReadLine();
                    if (addressInput.StartsWith("realms:"))
                    {
                        if (Settings.MinecraftRealmsEnabled)
                        {
                            if (availableWorlds.Count == 0)
                            {
                                HandleFailure(Translations.Get("error.realms.access_denied"), false, ChatBot.DisconnectReason.LoginRejected);
                                return;
                            }
                            int worldIndex = 0;
                            string worldId = addressInput.Split(':')[1];
                            if (!availableWorlds.Contains(worldId) && int.TryParse(worldId, out worldIndex) && worldIndex < availableWorlds.Count)
                                worldId = availableWorlds[worldIndex];
                            if (availableWorlds.Contains(worldId))
                            {
                                string RealmsAddress = ProtocolHandler.GetRealmsWorldServerAddress(worldId, Settings.Username, session.PlayerID, session.ID);
                                if (RealmsAddress != "")
                                {
                                    addressInput = RealmsAddress;
                                    isRealms = true;
                                    Settings.ServerVersion = MCHighestVersion;
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
                    Settings.SetServerIP(addressInput);
                }

                //Get server version
                int protocolversion = 0;
                ForgeInfo forgeInfo = null;

                if (Settings.ServerVersion != "" && Settings.ServerVersion.ToLower() != "auto")
                {
                    protocolversion = Protocol.ProtocolHandler.MCVer2ProtocolVersion(Settings.ServerVersion);

                    if (protocolversion != 0)
                    {
                        ConsoleIO.WriteLineFormatted(Translations.Get("mcc.use_version", Settings.ServerVersion, protocolversion));
                    }
                    else ConsoleIO.WriteLineFormatted(Translations.Get("mcc.unknown_version", Settings.ServerVersion));

                    if (useMcVersionOnce)
                    {
                        useMcVersionOnce = false;
                        Settings.ServerVersion = "";
                    }
                }

                //Retrieve server info if version is not manually set OR if need to retrieve Forge information
                if (!isRealms && (protocolversion == 0 || Settings.ServerAutodetectForge || (Settings.ServerForceForge && !ProtocolHandler.ProtocolMayForceForge(protocolversion))))
                {
                    if (protocolversion != 0)
                        Translations.WriteLine("mcc.forge");
                    else Translations.WriteLine("mcc.retrieve");
                    if (!ProtocolHandler.GetServerInfo(Settings.ServerIP, Settings.ServerPort, ref protocolversion, ref forgeInfo))
                    {
                        HandleFailure(Translations.Get("error.ping"), true, ChatBots.AutoRelog.DisconnectReason.ConnectionLost);
                        return;
                    }
                }

                //Force-enable Forge support?
                if (!isRealms && Settings.ServerForceForge && forgeInfo == null)
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
                        if (Settings.SingleCommand != "")
                        {
                            client = new McClient(session.PlayerName, session.PlayerID, session.ID, Settings.ServerIP, Settings.ServerPort, protocolversion, forgeInfo, Settings.SingleCommand);
                        }
                        else client = new McClient(session.PlayerName, session.PlayerID, session.ID, protocolversion, forgeInfo, Settings.ServerIP, Settings.ServerPort);

                        //Update console title
                        if (Settings.ConsoleTitle != "")
                            Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);
                    }
                    catch (NotSupportedException) { HandleFailure(Translations.Get("error.unsupported"), true); }
                }
                else HandleFailure(Translations.Get("error.determine"), true);
            }
            else
            {
                string failureMessage = Translations.Get("error.login");
                string failureReason = "";
                switch (result)
                {
                    case ProtocolHandler.LoginResult.AccountMigrated: failureReason = "error.login.migrated"; break;
                    case ProtocolHandler.LoginResult.ServiceUnavailable: failureReason = "error.login.server"; break;
                    case ProtocolHandler.LoginResult.WrongPassword: failureReason = "error.login.blocked"; break;
                    case ProtocolHandler.LoginResult.InvalidResponse: failureReason = "error.login.response"; break;
                    case ProtocolHandler.LoginResult.NotPremium: failureReason = "error.login.premium"; break;
                    case ProtocolHandler.LoginResult.OtherError: failureReason = "error.login.network"; break;
                    case ProtocolHandler.LoginResult.SSLError: failureReason = "error.login.ssl"; break;
                    case ProtocolHandler.LoginResult.UserCancel: failureReason = "error.login.cancel"; break;
                    default: failureReason = "error.login.unknown"; break;
                }
                failureMessage += Translations.Get(failureReason);

                if (result == ProtocolHandler.LoginResult.SSLError && isUsingMono)
                {
                    Translations.WriteLineFormatted("error.login.ssl_help");
                    return;
                }
                HandleFailure(failureMessage, false, ChatBot.DisconnectReason.LoginRejected);
            }
        }

        /// <summary>
        /// Disconnect the current client from the server and restart it
        /// </summary>
        /// <param name="delaySeconds">Optional delay, in seconds, before restarting</param>
        public static void Restart(int delaySeconds = 0)
        {
            new Thread(new ThreadStart(delegate
            {
                if (client != null) { client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Abort(); offlinePrompt = null; ConsoleIO.Reset(); }
                if (delaySeconds > 0)
                {
                    Translations.WriteLine("mcc.restart_delay", delaySeconds);
                    System.Threading.Thread.Sleep(delaySeconds * 1000);
                }
                Translations.WriteLine("mcc.restart");
                InitializeClient();
            })).Start();
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>
        public static void Exit(int exitcode = 0)
        {
            new Thread(new ThreadStart(delegate
            {
                if (client != null) { client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Abort(); offlinePrompt = null; ConsoleIO.Reset(); }
                if (Settings.playerHeadAsIcon) { ConsoleIcon.revertToMCCIcon(); }
                Environment.Exit(exitcode);
            })).Start();
        }

        /// <summary>
        /// Handle fatal errors such as ping failure, login failure, server disconnection, and so on.
        /// Allows AutoRelog to perform on fatal errors, prompt for server version, and offline commands.
        /// </summary>
        /// <param name="errorMessage">Error message to display and optionally pass to AutoRelog bot</param>
        /// <param name="versionError">Specify if the error is related to an incompatible or unkown server version</param>
        /// <param name="disconnectReason">If set, the error message will be processed by the AutoRelog bot</param>
        public static void HandleFailure(string errorMessage = null, bool versionError = false, ChatBots.AutoRelog.DisconnectReason? disconnectReason = null)
        {
            if (!String.IsNullOrEmpty(errorMessage))
            {
                ConsoleIO.Reset();
                while (Console.KeyAvailable)
                    Console.ReadKey(true);
                Console.WriteLine(errorMessage);

                if (disconnectReason.HasValue)
                {
                    if (ChatBots.AutoRelog.OnDisconnectStatic(disconnectReason.Value, errorMessage))
                        return; //AutoRelog is triggering a restart of the client
                }
            }

            if (Settings.interactiveMode)
            {
                if (versionError)
                {
                    Translations.Write("mcc.server_version");
                    Settings.ServerVersion = Console.ReadLine();
                    if (Settings.ServerVersion != "")
                    {
                        useMcVersionOnce = true;
                        Restart();
                        return;
                    }
                }

                if (offlinePrompt == null)
                {
                    offlinePrompt = new Thread(new ThreadStart(delegate
                    {
                        string command = " ";
                        ConsoleIO.WriteLineFormatted(Translations.Get("mcc.disconnected", (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar)));
                        Translations.WriteLineFormatted("mcc.press_exit");
                        while (command.Length > 0)
                        {
                            if (!ConsoleIO.BasicIO)
                            {
                                ConsoleIO.Write('>');
                            }
                            command = Console.ReadLine().Trim();
                            if (command.Length > 0)
                            {
                                string message = "";

                                if (Settings.internalCmdChar != ' '
                                    && command[0] == Settings.internalCmdChar)
                                    command = command.Substring(1);

                                if (command.StartsWith("reco"))
                                {
                                    message = new Commands.Reco().Run(null, Settings.ExpandVars(command), null);
                                }
                                else if (command.StartsWith("connect"))
                                {
                                    message = new Commands.Connect().Run(null, Settings.ExpandVars(command), null);
                                }
                                else if (command.StartsWith("exit") || command.StartsWith("quit"))
                                {
                                    message = new Commands.Exit().Run(null, Settings.ExpandVars(command), null);
                                }
                                else if (command.StartsWith("help"))
                                {
                                    ConsoleIO.WriteLineFormatted("§8MCC: " + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + new Commands.Reco().GetCmdDescTranslated());
                                    ConsoleIO.WriteLineFormatted("§8MCC: " + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + new Commands.Connect().GetCmdDescTranslated());
                                }
                                else ConsoleIO.WriteLineFormatted(Translations.Get("icmd.unknown", command.Split(' ')[0]));

                                if (message != "")
                                    ConsoleIO.WriteLineFormatted("§8MCC: " + message);
                            }
                        }
                    }));
                    offlinePrompt.Start();
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
        /// Detect if the user is running Minecraft Console Client through Mono
        /// </summary>
        public static bool isUsingMono
        {
            get
            {
                return Type.GetType("Mono.Runtime") != null;
            }
        }

        /// <summary>
        /// Enumerate types in namespace through reflection
        /// </summary>
        /// <param name="nameSpace">Namespace to process</param>
        /// <param name="assembly">Assembly to use. Default is Assembly.GetExecutingAssembly()</param>
        /// <returns></returns>
        public static Type[] GetTypesInNamespace(string nameSpace, Assembly assembly = null)
        {
            if (assembly == null) { assembly = Assembly.GetExecutingAssembly(); }
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }

        /// <summary>
        /// Static initialization of build information, read from assembly information
        /// </summary>
        static Program()
        {
            AssemblyConfigurationAttribute attribute
             = typeof(Program)
                .Assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
                .FirstOrDefault() as AssemblyConfigurationAttribute;
            if (attribute != null)
                BuildInfo = attribute.Configuration;
        }
    }
}
