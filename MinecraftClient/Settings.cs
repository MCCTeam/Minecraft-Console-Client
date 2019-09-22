using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Protocol;

namespace MinecraftClient
{
    /// <summary>
    /// Contains main settings for Minecraft Console Client
    /// Allows settings loading from an INI file
    /// </summary>

    public static class Settings
    {
        //Minecraft Console Client client information used for BrandInfo setting
        private const string MCCBrandInfo = "Minecraft-Console-Client/" + Program.Version;

        //Main Settings.
        //Login: Username or email adress used as login for Minecraft/Mojang account
        //Username: The actual username of the user, obtained after login to the account
        public static string Login = "";
        public static string Username = "";
        public static string Password = "";
        public static string ServerIP = "";
        public static ushort ServerPort = 25565;
        public static string ServerVersion = "";
        public static string SingleCommand = "";
        public static string ConsoleTitle = "";

        //Proxy Settings
        public static bool ProxyEnabledLogin = false;
        public static bool ProxyEnabledIngame = false;
        public static string ProxyHost = "";
        public static int ProxyPort = 0;
        public static Proxy.ProxyHandler.Type proxyType = Proxy.ProxyHandler.Type.HTTP;
        public static string ProxyUsername = "";
        public static string ProxyPassword = "";

        //Minecraft Settings
        public static bool MCSettings_Enabled = true;
        public static string MCSettings_Locale = "en_US";
        public static byte MCSettings_Difficulty = 0;
        public static byte MCSettings_RenderDistance = 8;
        public static byte MCSettings_ChatMode = 0;
        public static bool MCSettings_ChatColors = true;
        public static byte MCSettings_MainHand = 0;
        public static bool MCSettings_Skin_Hat = true;
        public static bool MCSettings_Skin_Cape = true;
        public static bool MCSettings_Skin_Jacket = false;
        public static bool MCSettings_Skin_Sleeve_Left = false;
        public static bool MCSettings_Skin_Sleeve_Right = false;
        public static bool MCSettings_Skin_Pants_Left = false;
        public static bool MCSettings_Skin_Pants_Right = false;
        public static byte MCSettings_Skin_All
        {
            get
            {
                return (byte)(
                      ((MCSettings_Skin_Cape ? 1 : 0) << 0)
                    | ((MCSettings_Skin_Jacket ? 1 : 0) << 1)
                    | ((MCSettings_Skin_Sleeve_Left ? 1 : 0) << 2)
                    | ((MCSettings_Skin_Sleeve_Right ? 1 : 0) << 3)
                    | ((MCSettings_Skin_Pants_Left ? 1 : 0) << 4)
                    | ((MCSettings_Skin_Pants_Right ? 1 : 0) << 5)
                    | ((MCSettings_Skin_Hat ? 1 : 0) << 6)
                );
            }
        }

        //Other Settings
        public static string TranslationsFile_FromMCDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\.minecraft\assets\objects\ed\eda1518b15c711cf6e75d99003bd87753f67fac4"; //MC 1.10 en_GB.lang
        public static string TranslationsFile_Website_Index = "https://s3.amazonaws.com/Minecraft.Download/indexes/1.13.json";
        public static string TranslationsFile_Website_Download = "http://resources.download.minecraft.net";
        public static TimeSpan splitMessageDelay = TimeSpan.FromSeconds(2);
        public static List<string> Bots_Owners = new List<string>();
        public static TimeSpan botMessageDelay = TimeSpan.FromSeconds(2);
        public static string Language = "en_GB";
        public static bool interactiveMode = true;
        public static char internalCmdChar = '/';
        public static bool playerHeadAsIcon = false;
        public static string chatbotLogFile = "";
        public static bool CacheScripts = true;
        public static string BrandInfo = MCCBrandInfo;
        public static bool DisplaySystemMessages = true;
        public static bool DisplayXPBarMessages = true;
        public static bool DisplayChatLinks = true;
        public static bool TerrainAndMovements = false;
        public static bool InventoryHandling = false;
        public static string PrivateMsgsCmdName = "tell";
        public static CacheType SessionCaching = CacheType.Disk;
        public static bool DebugMessages = false;
        public static bool ResolveSrvRecords = true;
        public static bool ResolveSrvRecordsShortTimeout = true;

        //AntiAFK Settings
        public static bool AntiAFK_Enabled = false;
        public static int AntiAFK_Delay = 600;
        public static string AntiAFK_Command = "/ping";

        //Hangman Settings
        public static bool Hangman_Enabled = false;
        public static bool Hangman_English = true;
        public static string Hangman_FileWords_EN = "hangman-en.txt";
        public static string Hangman_FileWords_FR = "hangman-fr.txt";

        //Alerts Settings
        public static bool Alerts_Enabled = false;
        public static bool Alerts_Beep_Enabled = true;
        public static string Alerts_MatchesFile = "alerts.txt";
        public static string Alerts_ExcludesFile = "alerts-exclude.txt";

        //ChatLog Settings
        public static bool ChatLog_Enabled = false;
        public static bool ChatLog_DateTime = true;
        public static string ChatLog_File = "chatlog.txt";
        public static ChatBots.ChatLog.MessageFilter ChatLog_Filter = ChatBots.ChatLog.MessageFilter.AllMessages;

        //PlayerListLog Settings
        public static bool PlayerLog_Enabled = false;
        public static string PlayerLog_File = "playerlog.txt";
        public static int PlayerLog_Delay = 600;

        //AutoRelog Settings
        public static bool AutoRelog_Enabled = false;
        public static int AutoRelog_Delay = 10;
        public static int AutoRelog_Retries = 3;
        public static string AutoRelog_KickMessagesFile = "kickmessages.txt";

        //Script Scheduler Settings
        public static bool ScriptScheduler_Enabled = false;
        public static string ScriptScheduler_TasksFile = "tasks.ini";

        //Remote Control
        public static bool RemoteCtrl_Enabled = false;
        public static bool RemoteCtrl_AutoTpaccept = true;
        public static bool RemoteCtrl_AutoTpaccept_Everyone = false;

        //Chat Message Parsing
        public static bool ChatFormat_Builtins = true;
        public static Regex ChatFormat_Public = null;
        public static Regex ChatFormat_Private = null;
        public static Regex ChatFormat_TeleportRequest = null;

        //Auto Respond
        public static bool AutoRespond_Enabled = false;
        public static string AutoRespond_Matches = "matches.ini";

        //Custom app variables and Minecraft accounts
        private static readonly Dictionary<string, object> AppVars = new Dictionary<string, object>();
        private static readonly Dictionary<string, KeyValuePair<string, string>> Accounts = new Dictionary<string, KeyValuePair<string, string>>();
        private static readonly Dictionary<string, KeyValuePair<string, ushort>> Servers = new Dictionary<string, KeyValuePair<string, ushort>>();

        private enum ParseMode { Default, Main, AppVars, Proxy, MCSettings, AntiAFK, Hangman, Alerts, ChatLog, AutoRelog, ScriptScheduler, RemoteControl, ChatFormat, AutoRespond };

        /// <summary>
        /// Load settings from the give INI file
        /// </summary>
        /// <param name="settingsfile">File to load</param>
        public static void LoadSettings(string settingsfile)
        {
            ConsoleIO.WriteLogLine(String.Format("[Settings] Loading Settings from {0}", System.IO.Path.GetFullPath(settingsfile)));
            if (File.Exists(settingsfile))
            {
                try
                {
                    string serverAlias = "";
                    string[] Lines = File.ReadAllLines(settingsfile);
                    ParseMode pMode = ParseMode.Default;
                    foreach (string lineRAW in Lines)
                    {
                        string line = pMode == ParseMode.Main && lineRAW.ToLower().Trim().StartsWith("password")
                            ? lineRAW.Trim() //Do not strip # in passwords
                            : lineRAW.Split('#')[0].Trim();

                        if (line.Length > 0)
                        {
                            if (line[0] == '[' && line[line.Length - 1] == ']')
                            {
                                switch (line.Substring(1, line.Length - 2).ToLower())
                                {
                                    case "alerts": pMode = ParseMode.Alerts; break;
                                    case "antiafk": pMode = ParseMode.AntiAFK; break;
                                    case "autorelog": pMode = ParseMode.AutoRelog; break;
                                    case "chatlog": pMode = ParseMode.ChatLog; break;
                                    case "hangman": pMode = ParseMode.Hangman; break;
                                    case "main": pMode = ParseMode.Main; break;
                                    case "mcsettings": pMode = ParseMode.MCSettings; break;
                                    case "scriptscheduler": pMode = ParseMode.ScriptScheduler; break;
                                    case "remotecontrol": pMode = ParseMode.RemoteControl; break;
                                    case "proxy": pMode = ParseMode.Proxy; break;
                                    case "appvars": pMode = ParseMode.AppVars; break;
                                    case "autorespond": pMode = ParseMode.AutoRespond; break;
                                    case "chatformat": pMode = ParseMode.ChatFormat; break;
                                    default: pMode = ParseMode.Default; break;
                                }
                            }
                            else
                            {
                                string argName = line.Split('=')[0];
                                if (line.Length > (argName.Length + 1))
                                {
                                    string argValue = line.Substring(argName.Length + 1);
                                    switch (pMode)
                                    {
                                        case ParseMode.Main:
                                            switch (argName.ToLower())
                                            {
                                                case "login": Login = argValue; break;
                                                case "password": Password = argValue; break;
                                                case "serverip": if (!SetServerIP(argValue)) serverAlias = argValue; ; break;
                                                case "singlecommand": SingleCommand = argValue; break;
                                                case "language": Language = argValue; break;
                                                case "consoletitle": ConsoleTitle = argValue; break;
                                                case "timestamps": ConsoleIO.EnableTimestamps = str2bool(argValue); break;
                                                case "exitonfailure": interactiveMode = !str2bool(argValue); break;
                                                case "playerheadicon": playerHeadAsIcon = str2bool(argValue); break;
                                                case "chatbotlogfile": chatbotLogFile = argValue; break;
                                                case "mcversion": ServerVersion = argValue; break;
                                                case "splitmessagedelay": splitMessageDelay = TimeSpan.FromSeconds(str2int(argValue)); break;
                                                case "scriptcache": CacheScripts = str2bool(argValue); break;
                                                case "showsystemmessages": DisplaySystemMessages = str2bool(argValue); break;
                                                case "showxpbarmessages": DisplayXPBarMessages = str2bool(argValue); break;
                                                case "showchatlinks": DisplayChatLinks = str2bool(argValue); break;
                                                case "terrainandmovements": TerrainAndMovements = str2bool(argValue); break;
                                                case "inventoryhandling": InventoryHandling = str2bool(argValue); break;
                                                case "privatemsgscmdname": PrivateMsgsCmdName = argValue.ToLower().Trim(); break;
                                                case "botmessagedelay": botMessageDelay = TimeSpan.FromSeconds(str2int(argValue)); break;
                                                case "debugmessages": DebugMessages = str2bool(argValue); break;

                                                case "botowners":
                                                    Bots_Owners.Clear();
                                                    string[] names = argValue.ToLower().Split(',');
                                                    if (!argValue.Contains(",") && argValue.ToLower().EndsWith(".txt") && File.Exists(argValue))
                                                        names = File.ReadAllLines(argValue);
                                                    foreach (string name in names)
                                                        if (!String.IsNullOrWhiteSpace(name))
                                                            Bots_Owners.Add(name.Trim());
                                                    break;

                                                case "internalcmdchar":
                                                    switch (argValue.ToLower())
                                                    {
                                                        case "none": internalCmdChar = ' '; break;
                                                        case "slash": internalCmdChar = '/'; break;
                                                        case "backslash": internalCmdChar = '\\'; break;
                                                    }
                                                    break;

                                                case "sessioncache":
                                                    if (argValue == "none") { SessionCaching = CacheType.None; }
                                                    else if (argValue == "memory") { SessionCaching = CacheType.Memory; }
                                                    else if (argValue == "disk") { SessionCaching = CacheType.Disk; }
                                                    break;

                                                case "accountlist":
                                                    if (File.Exists(argValue))
                                                    {
                                                        foreach (string account_line in File.ReadAllLines(argValue))
                                                        {
                                                            //Each line contains account data: 'Alias,Login,Password'
                                                            string[] account_data = account_line.Split('#')[0].Trim().Split(',');
                                                            if (account_data.Length == 3)
                                                                Accounts[account_data[0].ToLower()]
                                                                    = new KeyValuePair<string, string>(account_data[1], account_data[2]);
                                                        }

                                                        //Try user value against aliases after load
                                                        Settings.SetAccount(Login);
                                                    }
                                                    break;

                                                case "serverlist":
                                                    if (File.Exists(argValue))
                                                    {
                                                        //Backup current server info
                                                        string server_host_temp = ServerIP;
                                                        ushort server_port_temp = ServerPort;

                                                        foreach (string server_line in File.ReadAllLines(argValue))
                                                        {
                                                            //Each line contains server data: 'Alias,Host:Port'
                                                            string[] server_data = server_line.Split('#')[0].Trim().Split(',');
                                                            server_data[0] = server_data[0].ToLower();
                                                            if (server_data.Length == 2
                                                                && server_data[0] != "localhost"
                                                                && !server_data[0].Contains('.')
                                                                && SetServerIP(server_data[1]))
                                                                Servers[server_data[0]]
                                                                    = new KeyValuePair<string, ushort>(ServerIP, ServerPort);
                                                        }

                                                        //Restore current server info
                                                        ServerIP = server_host_temp;
                                                        ServerPort = server_port_temp;

                                                        //Try server value against aliases after load
                                                        SetServerIP(serverAlias);
                                                    }
                                                    break;

                                                case "brandinfo":
                                                    switch (argValue.Trim().ToLower())
                                                    {
                                                        case "mcc": BrandInfo = MCCBrandInfo; break;
                                                        case "vanilla": BrandInfo = "vanilla"; break;
                                                        default: BrandInfo = null; break;
                                                    }
                                                    break;

                                                case "resolvesrvrecords":
                                                    if (argValue.Trim().ToLower() == "fast")
                                                    {
                                                        ResolveSrvRecords = true;
                                                        ResolveSrvRecordsShortTimeout = true;
                                                    }
                                                    else
                                                    {
                                                        ResolveSrvRecords = str2bool(argValue);
                                                        ResolveSrvRecordsShortTimeout = false;
                                                    }
                                                    break;
                                            }
                                            break;

                                        case ParseMode.Alerts:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": Alerts_Enabled = str2bool(argValue); break;
                                                case "alertsfile": Alerts_MatchesFile = argValue; break;
                                                case "excludesfile": Alerts_ExcludesFile = argValue; break;
                                                case "beeponalert": Alerts_Beep_Enabled = str2bool(argValue); break;
                                            }
                                            break;

                                        case ParseMode.AntiAFK:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": AntiAFK_Enabled = str2bool(argValue); break;
                                                case "delay": AntiAFK_Delay = str2int(argValue); break;
                                                case "command": AntiAFK_Command = argValue == "" ? "/ping" : argValue; break;
                                            }
                                            break;

                                        case ParseMode.AutoRelog:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": AutoRelog_Enabled = str2bool(argValue); break;
                                                case "delay": AutoRelog_Delay = str2int(argValue); break;
                                                case "retries": AutoRelog_Retries = str2int(argValue); break;
                                                case "kickmessagesfile": AutoRelog_KickMessagesFile = argValue; break;
                                            }
                                            break;

                                        case ParseMode.ChatLog:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": ChatLog_Enabled = str2bool(argValue); break;
                                                case "timestamps": ChatLog_DateTime = str2bool(argValue); break;
                                                case "filter": ChatLog_Filter = ChatBots.ChatLog.str2filter(argValue); break;
                                                case "logfile": ChatLog_File = argValue; break;
                                            }
                                            break;

                                        case ParseMode.Hangman:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": Hangman_Enabled = str2bool(argValue); break;
                                                case "english": Hangman_English = str2bool(argValue); break;
                                                case "wordsfile": Hangman_FileWords_EN = argValue; break;
                                                case "fichiermots": Hangman_FileWords_FR = argValue; break;
                                            }
                                            break;

                                        case ParseMode.ScriptScheduler:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": ScriptScheduler_Enabled = str2bool(argValue); break;
                                                case "tasksfile": ScriptScheduler_TasksFile = argValue; break;
                                            }
                                            break;

                                        case ParseMode.RemoteControl:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": RemoteCtrl_Enabled = str2bool(argValue); break;
                                                case "autotpaccept": RemoteCtrl_AutoTpaccept = str2bool(argValue); break;
                                                case "tpaccepteveryone": RemoteCtrl_AutoTpaccept_Everyone = str2bool(argValue); break;
                                            }
                                            break;

                                        case ParseMode.ChatFormat:
                                            switch (argName.ToLower())
                                            {
                                                case "builtins": ChatFormat_Builtins = str2bool(argValue); break;
                                                case "public": ChatFormat_Public = new Regex(argValue); break;
                                                case "private": ChatFormat_Private = new Regex(argValue); break;
                                                case "tprequest": ChatFormat_TeleportRequest = new Regex(argValue); break;
                                            }
                                            break;

                                        case ParseMode.Proxy:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled":
                                                    ProxyEnabledLogin = ProxyEnabledIngame = str2bool(argValue);
                                                    if (argValue.Trim().ToLower() == "login")
                                                        ProxyEnabledLogin = true;
                                                    break;
                                                case "type":
                                                    argValue = argValue.ToLower();
                                                    if (argValue == "http") { proxyType = Proxy.ProxyHandler.Type.HTTP; }
                                                    else if (argValue == "socks4") { proxyType = Proxy.ProxyHandler.Type.SOCKS4; }
                                                    else if (argValue == "socks4a") { proxyType = Proxy.ProxyHandler.Type.SOCKS4a; }
                                                    else if (argValue == "socks5") { proxyType = Proxy.ProxyHandler.Type.SOCKS5; }
                                                    break;
                                                case "server":
                                                    string[] host_splitted = argValue.Split(':');
                                                    if (host_splitted.Length == 1)
                                                    {
                                                        ProxyHost = host_splitted[0];
                                                        ProxyPort = 80;
                                                    }
                                                    else if (host_splitted.Length == 2)
                                                    {
                                                        ProxyHost = host_splitted[0];
                                                        ProxyPort = str2int(host_splitted[1]);
                                                    }
                                                    break;
                                                case "username": ProxyUsername = argValue; break;
                                                case "password": ProxyPassword = argValue; break;
                                            }
                                            break;

                                        case ParseMode.AppVars:
                                            SetVar(argName, argValue);
                                            break;

                                        case ParseMode.AutoRespond:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": AutoRespond_Enabled = str2bool(argValue); break;
                                                case "matchesfile": AutoRespond_Matches = argValue; break;
                                            }
                                            break;

                                        case ParseMode.MCSettings:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": MCSettings_Enabled = str2bool(argValue); break;
                                                case "locale": MCSettings_Locale = argValue; break;
                                                case "difficulty":
                                                    switch (argValue.ToLower())
                                                    {
                                                        case "peaceful": MCSettings_Difficulty = 0; break;
                                                        case "easy": MCSettings_Difficulty = 1; break;
                                                        case "normal": MCSettings_Difficulty = 2; break;
                                                        case "difficult": MCSettings_Difficulty = 3; break;
                                                    }
                                                    break;
                                                case "renderdistance":
                                                    MCSettings_RenderDistance = 2;
                                                    if (argValue.All(Char.IsDigit))
                                                    {
                                                        MCSettings_RenderDistance = (byte)str2int(argValue);
                                                    }
                                                    else
                                                    {
                                                        switch (argValue.ToLower())
                                                        {
                                                            case "tiny": MCSettings_RenderDistance = 2; break;
                                                            case "short": MCSettings_RenderDistance = 4; break;
                                                            case "medium": MCSettings_RenderDistance = 8; break;
                                                            case "far": MCSettings_RenderDistance = 16; break;
                                                        }
                                                    }
                                                    break;
                                                case "chatmode":
                                                    switch (argValue.ToLower())
                                                    {
                                                        case "enabled": MCSettings_ChatMode = 0; break;
                                                        case "commands": MCSettings_ChatMode = 1; break;
                                                        case "disabled": MCSettings_ChatMode = 2; break;
                                                    }
                                                    break;
                                                case "chatcolors": MCSettings_ChatColors = str2bool(argValue); break;
                                                case "skin_cape": MCSettings_Skin_Cape = str2bool(argValue); break;
                                                case "skin_jacket": MCSettings_Skin_Jacket = str2bool(argValue); break;
                                                case "skin_sleeve_left": MCSettings_Skin_Sleeve_Left = str2bool(argValue); break;
                                                case "skin_sleeve_right": MCSettings_Skin_Sleeve_Right = str2bool(argValue); break;
                                                case "skin_pants_left": MCSettings_Skin_Pants_Left = str2bool(argValue); break;
                                                case "skin_pants_right": MCSettings_Skin_Pants_Right = str2bool(argValue); break;
                                                case "skin_hat": MCSettings_Skin_Hat = str2bool(argValue); break;
                                                case "main_hand":
                                                    switch (argValue.ToLower())
                                                    {
                                                        case "left": MCSettings_MainHand = 0; break;
                                                        case "right": MCSettings_MainHand = 1; break;
                                                    }
                                                    break;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (IOException) { }
            }
        }

        /// <summary>
        /// Write an INI file with default settings
        /// </summary>
        /// <param name="settingsfile">File to (over)write</param>
        public static void WriteDefaultSettings(string settingsfile)
        {
            System.IO.File.WriteAllText(settingsfile, "# Minecraft Console Client v" + Program.Version + "\r\n"
                + "# Startup Config File\r\n"
                + "\r\n"
                + "[Main]\r\n"
                + "\r\n"
                + "# General settings\r\n"
                + "# Leave blank to prompt user on startup\r\n"
                + "# Use \"-\" as password for offline mode\r\n"
                + "\r\n"
                + "login=\r\n"
                + "password=\r\n"
                + "serverip=\r\n"
                + "\r\n"
                + "# Advanced settings\r\n"
                + "\r\n"
                + "language=en_GB\r\n"
                + "consoletitle=%username%@%serverip% - Minecraft Console Client\r\n"
                + "internalcmdchar=slash              # Use 'none', 'slash' or 'backslash'\r\n"
                + "splitmessagedelay=2                # Seconds between each part of a long message\r\n"
                + "botowners=Player1,Player2,Player3  # Use name list or myfile.txt with one name per line\r\n"
                + "botmessagedelay=2                  # Seconds to delay between message a bot makes to avoid accidental spam\r\n"
                + "mcversion=auto                     # Use 'auto' or '1.X.X' values\r\n"
                + "brandinfo=mcc                      # Use 'mcc','vanilla', or 'none'\r\n"
                + "chatbotlogfile=                    # Leave empty for no logfile\r\n"
                + "privatemsgscmdname=tell            # Used by RemoteControl bot\r\n"
                + "showsystemmessages=true            # System messages for server ops\r\n"
                + "showxpbarmessages=true             # Messages displayed above xp bar\r\n"
                + "showchatlinks=true                 # Show links embedded in chat messages\r\n"
                + "terrainandmovements=false          # Uses more ram, cpu, bandwidth\r\n"
                + "inventoryhandling=false            # Toggle inventory handling\r\n"
                + "sessioncache=disk                  # How to retain session tokens. Use 'none', 'memory' or 'disk'\r\n"
                + "resolvesrvrecords=fast             # Use 'false', 'fast' (5s timeout), or 'true'. Required for joining some servers.\r\n"
                + "accountlist=accounts.txt           # See README > 'Servers and Accounts file' for more info about this file\r\n"
                + "serverlist=servers.txt             # See README > 'Servers and Accounts file' for more info about this file\r\n"
                + "playerheadicon=true                # Only works on Windows XP-8 or Windows 10 with old console\r\n"
                + "exitonfailure=false                # Disable pauses on error, for using MCC in non-interactive scripts\r\n"
                + "debugmessages=false                # Please enable this before submitting bug reports. Thanks!\r\n"
                + "scriptcache=true                   # Cache compiled scripts for faster load on low-end devices\r\n"
                + "timestamps=false                   # Prepend timestamps to chat messages\r\n"
                + "\r\n"
                + "[AppVars]\r\n"
                + "# yourvar=yourvalue\r\n"
                + "# can be used in some other fields as %yourvar%\r\n"
                + "# %username% and %serverip% are reserved variables.\r\n"
                + "\r\n"
                + "[Proxy]\r\n"
                + "enabled=false                      # Use 'false', 'true', or 'login' for login only\r\n"
                + "type=HTTP                          # Supported types: HTTP, SOCKS4, SOCKS4a, SOCKS5\r\n"
                + "server=0.0.0.0:0000                # Proxy server must allow HTTPS for login, and non-443 ports for playing\r\n"
                + "username=                          # Only required for password-protected proxies\r\n"
                + "password=                          # Only required for password-protected proxies\r\n"
                + "\r\n"
                + "[ChatFormat]\r\n"
                + "# Do not forget to uncomment (remove '#') these settings if modifying them\r\n"
                + "builtins=true                      # MCC built-in support for common message formats\r\n"
                + "# public=^<([a-zA-Z0-9_]+)> (.+)$\r\n"
                + "# private=^([a-zA-Z0-9_]+) whispers to you: (.+)$\r\n"
                + "# tprequest=^([a-zA-Z0-9_]+) has requested (?:to|that you) teleport to (?:you|them)\\.$\r\n"
                + "\r\n"
                + "[MCSettings]\r\n"
                + "enabled=true                       # If disabled, settings below are not sent to the server\r\n"
                + "locale=en_US                       # Use any language implemented in Minecraft\r\n"
                + "renderdistance=medium              # Use tiny, short, medium, far, or chunk amount [0 - 255]\r\n"
                + "difficulty=normal                  # MC 1.7- difficulty. peaceful, easy, normal, difficult\r\n"
                + "chatmode=enabled                   # Use 'enabled', 'commands', or 'disabled'. Allows to mute yourself...\r\n"
                + "chatcolors=true                    # Allows disabling chat colors server-side\r\n"
                + "main_hand=left                     # MC 1.9+ main hand. left or right\r\n"
                + "skin_cape=true\r\n"
                + "skin_hat=true\r\n"
                + "skin_jacket=false\r\n"
                + "skin_sleeve_left=false\r\n"
                + "skin_sleeve_right=false\r\n"
                + "skin_pants_left=false\r\n"
                + "skin_pants_right=false"
                + "\r\n"
                + "# Bot Settings\r\n"
                + "\r\n"
                + "[Alerts]\r\n"
                + "enabled=false\r\n"
                + "alertsfile=alerts.txt\r\n"
                + "excludesfile=alerts-exclude.txt\r\n"
                + "beeponalert=true\r\n"
                + "\r\n"
                + "[AntiAFK]\r\n"
                + "enabled=false\r\n"
                + "delay=600 #10 = 1s\r\n"
                + "command=/ping\r\n"
                + "\r\n"
                + "[AutoRelog]\r\n"
                + "enabled=false\r\n"
                + "delay=10\r\n"
                + "retries=3 #-1 = unlimited\r\n"
                + "kickmessagesfile=kickmessages.txt\r\n"
                + "\r\n"
                + "[ChatLog]\r\n"
                + "enabled=false\r\n"
                + "timestamps=true\r\n"
                + "filter=messages\r\n"
                + "logfile=chatlog-%username%-%serverip%.txt\r\n"
                + "\r\n"
                + "[Hangman]\r\n"
                + "enabled=false\r\n"
                + "english=true\r\n"
                + "wordsfile=hangman-en.txt\r\n"
                + "fichiermots=hangman-fr.txt\r\n"
                + "\r\n"
                + "[ScriptScheduler]\r\n"
                + "enabled=false\r\n"
                + "tasksfile=tasks.ini\r\n"
                + "\r\n"
                + "[RemoteControl]\r\n"
                + "enabled=false\r\n"
                + "autotpaccept=true\r\n"
                + "tpaccepteveryone=false\r\n"
                + "\r\n"
                + "[AutoRespond]\r\n"
                + "enabled=false\r\n"
                + "matchesfile=matches.ini\r\n", Encoding.UTF8);
        }

        /// <summary>
        /// Convert the specified string to an integer, defaulting to zero if invalid argument
        /// </summary>
        /// <param name="str">String to parse as an integer</param>
        /// <returns>Integer value</returns>
        public static int str2int(string str)
        {
            try
            {
                return Convert.ToInt32(str.Trim());
            }
            catch {
                ConsoleIO.WriteLogLine("Failed to convert '" + str + "' into an int. Please check your settings.");
                return 0;
            }
        }

        /// <summary>
        /// Convert the specified string to a boolean value, defaulting to false if invalid argument
        /// </summary>
        /// <param name="str">String to parse as a boolean</param>
        /// <returns>Boolean value</returns>
        public static bool str2bool(string str)
        {
            if (String.IsNullOrEmpty(str))
                return false;
            str = str.Trim().ToLowerInvariant();
            return str == "true" || str == "1";
        }

        /// <summary>
        /// Load login/password using an account alias
        /// </summary>
        /// <returns>True if the account was found and loaded</returns>
        public static bool SetAccount(string accountAlias)
        {
            accountAlias = accountAlias.ToLower();
            if (Accounts.ContainsKey(accountAlias))
            {
                Settings.Login = Accounts[accountAlias].Key;
                Settings.Password = Accounts[accountAlias].Value;
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Load server information in ServerIP and ServerPort variables from a "serverip:port" couple or server alias
        /// </summary>
        /// <returns>True if the server IP was valid and loaded, false otherwise</returns>
        public static bool SetServerIP(string server)
        {
            server = server.ToLower();
            string[] sip = server.Split(':');
            string host = sip[0];
            ushort port = 25565;

            if (sip.Length > 1)
            {
                try
                {
                    port = Convert.ToUInt16(sip[1]);
                }
                catch (FormatException) { return false; }
            }

            if (host == "localhost" || host.Contains('.'))
            {
                //Server IP (IP or domain names contains at least a dot)
                if (sip.Length == 1 && host.Contains('.') && host.Any(c => char.IsLetter(c)) && ResolveSrvRecords)
                    //Domain name without port may need Minecraft SRV Record lookup
                    ProtocolHandler.MinecraftServiceLookup(ref host, ref port);
                ServerIP = host;
                ServerPort = port;
                return true;
            }
            else if (Servers.ContainsKey(server))
            {
                //Server Alias (if no dot then treat the server as an alias)
                ServerIP = Servers[server].Key;
                ServerPort = Servers[server].Value;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set a custom %variable% which will be available through expandVars()
        /// </summary>
        /// <param name="varName">Name of the variable</param>
        /// <param name="varData">Value of the variable</param>
        /// <returns>True if the parameters were valid</returns>
        public static bool SetVar(string varName, object varData)
        {
            lock (AppVars)
            {
                varName = new string(varName.TakeWhile(char.IsLetterOrDigit).ToArray()).ToLower();
                if (varName.Length > 0)
                {
                    AppVars[varName] = varData;
                    return true;
                }
                else return false;
            }
        }

        /// <summary>
        /// Get a custom %variable% or null if the variable does not exist
        /// </summary>
        /// <param name="varName">Variable name</param>
        /// <returns>The value or null if the variable does not exists</returns>
        public static object GetVar(string varName)
        {
            if (AppVars.ContainsKey(varName))
                return AppVars[varName];
            return null;
        }

        /// <summary>
        /// Replace %variables% with their value
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <returns>Modifier string</returns>
        public static string ExpandVars(string str)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '%')
                {
                    bool varname_ok = false;
                    StringBuilder var_name = new StringBuilder();

                    for (int j = i + 1; j < str.Length; j++)
                    {
                        if (!char.IsLetterOrDigit(str[j]))
                        {
                            if (str[j] == '%')
                                varname_ok = var_name.Length > 0;
                            break;
                        }
                        else var_name.Append(str[j]);
                    }

                    if (varname_ok)
                    {
                        string varname = var_name.ToString();
                        string varname_lower = varname.ToLower();
                        i = i + varname.Length + 1;

                        switch (varname_lower)
                        {
                            case "username": result.Append(Username); break;
                            case "serverip": result.Append(ServerIP); break;
                            case "serverport": result.Append(ServerPort); break;
                            default:
                                if (AppVars.ContainsKey(varname_lower))
                                {
                                    result.Append(AppVars[varname_lower].ToString());
                                }
                                else result.Append("%" + varname + '%');
                                break;
                        }
                    }
                    else result.Append(str[i]);
                }
                else result.Append(str[i]);
            }
            return result.ToString();
        }
    }
}
