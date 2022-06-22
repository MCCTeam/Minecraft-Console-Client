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
        public static ProtocolHandler.AccountType AccountType = ProtocolHandler.AccountType.Mojang;
        public static string LoginMethod = "mcc";
        public static string ServerIP = "";
        public static ushort ServerPort = 25565;
        public static string ServerVersion = "";
        public static bool ServerForceForge = false;
        public static bool ServerAutodetectForge = true;
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
        public static string TranslationsFile_FromMCDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\.minecraft\assets\objects\8b\8bf1298bd44b0e5b21d747394a8acd2c218e09ed"; //MC 1.17 en_GB.lang
        public static string TranslationsFile_Website_Index = "https://launchermeta.mojang.com/v1/packages/e5af543d9b3ce1c063a97842c38e50e29f961f00/1.17.json";
        public static string TranslationsFile_Website_Download = "http://resources.download.minecraft.net";
        public static TimeSpan messageCooldown = TimeSpan.FromSeconds(2);
        public static List<string> Bots_Owners = new List<string>();
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
        public static bool DisplayInventoryLayout = true;
        public static bool TerrainAndMovements = false;
        public static bool GravityEnabled = true;
        public static bool InventoryHandling = false;
        public static string PrivateMsgsCmdName = "tell";
        public static CacheType SessionCaching = CacheType.Disk;
        public static bool ResolveSrvRecords = true;
        public static bool ResolveSrvRecordsShortTimeout = true;
        public static bool EntityHandling = false;
        public static bool AutoRespawn = false;
        public static bool MinecraftRealmsEnabled = true;
        public static bool MoveHeadWhileWalking = true;

        // Logging
        public enum FilterModeEnum { Blacklist, Whitelist }
        public static bool DebugMessages = false;
        public static bool ChatMessages = true;
        public static bool InfoMessages = true;
        public static bool WarningMessages = true;
        public static bool ErrorMessages = true;
        public static Regex ChatFilter = null;
        public static Regex DebugFilter = null;
        public static FilterModeEnum FilterMode = FilterModeEnum.Blacklist;
        public static bool LogToFile = false;
        public static string LogFile = "console-log.txt";
        public static bool PrependTimestamp = false;
        public static bool SaveColorCodes = false;

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
        public static int AutoRelog_Delay_Min = 10;
        public static int AutoRelog_Delay_Max = 10;
        public static int AutoRelog_Retries = 3;
        public static bool AutoRelog_IgnoreKickMessage = false;
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

        //Auto Attack
        public static bool AutoAttack_Enabled = false;
        public static string AutoAttack_Mode = "single";
        public static string AutoAttack_Priority = "distance";
        public static bool AutoAttack_OverrideAttackSpeed = false;
        public static double AutoAttack_CooldownSeconds = 1;

        //Auto Fishing
        public static bool AutoFishing_Enabled = false;
        public static bool AutoFishing_Antidespawn = false;

        //Auto Eating
        public static bool AutoEat_Enabled = false;
        public static int AutoEat_hungerThreshold = 6;

        //AutoCraft
        public static bool AutoCraft_Enabled = false;
        public static string AutoCraft_configFile = @"autocraft\config.ini";
        
        //Mailer
        public static bool Mailer_Enabled = false;
        public static string Mailer_DatabaseFile = "MailerDatabase.ini";
        public static string Mailer_IgnoreListFile = "MailerIgnoreList.ini";
        public static bool Mailer_PublicInteractions = false;
        public static int Mailer_MaxMailsPerPlayer = 10;
        public static int Mailer_MaxDatabaseSize = 10000;
        public static int Mailer_MailRetentionDays = 30;

        //AutoDrop
        public static bool AutoDrop_Enabled = false;
        public static string AutoDrop_Mode = "include";
        public static string AutoDrop_items = "";

        // Replay Mod
        public static bool ReplayMod_Enabled = false;
        public static int ReplayMod_BackupInterval = 3000;

        //Custom app variables and Minecraft accounts
        private static readonly Dictionary<string, object> AppVars = new Dictionary<string, object>();
        private static readonly Dictionary<string, KeyValuePair<string, string>> Accounts = new Dictionary<string, KeyValuePair<string, string>>();
        private static readonly Dictionary<string, KeyValuePair<string, ushort>> Servers = new Dictionary<string, KeyValuePair<string, ushort>>();

        //Temporary Server Alias storage when server list is not loaded yet
        private static string ServerAliasTemp = null;

        //Mapping for settings sections in the INI file
        private enum Section { Default, Main, AppVars, Proxy, MCSettings, AntiAFK, Hangman, Alerts, ChatLog, AutoRelog, ScriptScheduler, RemoteControl, ChatFormat, AutoRespond, AutoAttack, AutoFishing, AutoEat, AutoCraft, AutoDrop, Mailer, ReplayMod, Logging };

        /// <summary>
        /// Get settings section from name
        /// </summary>
        /// <param name="name">Section name</param>
        /// <returns>Section enum</returns>
        private static Section GetSection(string name)
        {
            Section pMode;
            if (Enum.TryParse(name, true, out pMode))
                return pMode;
            return Section.Default;
        }

        /// <summary>
        /// Load settings from the given INI file
        /// </summary>
        /// <param name="file">File to load</param>
        public static void LoadFile(string file)
        {
            ConsoleIO.WriteLogLine("[Settings] Loading Settings from " + Path.GetFullPath(file));
            if (File.Exists(file))
            {
                try
                {
                    string[] Lines = File.ReadAllLines(file);
                    Section section = Section.Default;
                    foreach (string lineRAW in Lines)
                    {
                        string line = section == Section.Main && lineRAW.ToLower().Trim().StartsWith("password")
                            ? lineRAW.Trim() //Do not strip # in passwords
                            : lineRAW.Split('#')[0].Trim();

                        if (line.Length > 0)
                        {
                            if (line[0] == '[' && line[line.Length - 1] == ']')
                            {
                                section = GetSection(line.Substring(1, line.Length - 2).ToLower());
                            }
                            else
                            {
                                string argName = line.Split('=')[0];
                                if (line.Length > (argName.Length + 1))
                                {
                                    string argValue = line.Substring(argName.Length + 1);
                                    LoadSingleSetting(section, argName, argValue);
                                }
                            }
                        }
                    }
                }
                catch (IOException) { }
            }
        }

        /// <summary>
        /// Load settings from the command line
        /// </summary>
        /// <param name="args">Command-line arguments</param>
        /// <exception cref="System.ArgumentException">Thrown on invalid arguments</exception>
        public static void LoadArguments(string[] args)
        {
            int positionalIndex = 0;

            foreach (string argument in args)
            {
                if (argument.StartsWith("--"))
                {
                    //Load settings as --setting=value and --section.setting=value
                    if (!argument.Contains("="))
                        throw new ArgumentException(Translations.Get("error.setting.argument_syntax", argument));
                    Section section = Section.Main;
                    string argName = argument.Substring(2).Split('=')[0];
                    string argValue = argument.Substring(argName.Length + 3);
                    if (argName.Contains('.'))
                    {
                        string sectionName = argName.Split('.')[0];
                        section = GetSection(sectionName);
                        if (section == Section.Default)
                            throw new ArgumentException(Translations.Get("error.setting.unknown_section", argument, sectionName));
                        argName = argName.Split('.')[1];
                    }
                    if (!LoadSingleSetting(section, argName, argValue))
                        throw new ArgumentException(Translations.Get("error.setting.unknown_or_invalid", argument));
                }
                else if (argument.StartsWith("-") && argument.Length > 1)
                {
                    //Keep single dash arguments as unsupported for now (future use)
                    throw new ArgumentException(Translations.Get("error.setting.argument_syntax", argument));
                }
                else
                {
                    switch (positionalIndex)
                    {
                        case 0: Login = argument; break;
                        case 1: Password = argument; break;
                        case 2: if (!SetServerIP(argument)) ServerAliasTemp = argument; break;
                        case 3: SingleCommand = argument; break;
                    }
                    positionalIndex++;
                }
            }
        }

        /// <summary>
        /// Load a single setting from INI file or command-line argument
        /// </summary>
        /// <param name="section">Settings section</param>
        /// <param name="argName">Setting name</param>
        /// <param name="argValue">Setting value</param>
        /// <returns>TRUE if setting was valid</returns>
        private static bool LoadSingleSetting(Section section, string argName, string argValue)
        {
            switch (section)
            {
                case Section.Main:
                    switch (argName.ToLower())
                    {
                        case "login": Login = argValue; return true;
                        case "password": Password = argValue; return true;
                        case "type": AccountType = argValue == "mojang"
                                ? ProtocolHandler.AccountType.Mojang
                                : ProtocolHandler.AccountType.Microsoft; return true;
                        case "method": LoginMethod = argValue.ToLower() == "browser"
                                ? "browser"
                                : "mcc"; return true;
                        case "serverip": if (!SetServerIP(argValue)) ServerAliasTemp = argValue; return true;
                        case "singlecommand": SingleCommand = argValue; return true;
                        case "language": Language = argValue; return true;
                        case "consoletitle": ConsoleTitle = argValue; return true;
                        case "timestamps": ConsoleIO.EnableTimestamps = str2bool(argValue); return true;
                        case "exitonfailure": interactiveMode = !str2bool(argValue); return true;
                        case "playerheadicon": playerHeadAsIcon = str2bool(argValue); return true;
                        case "chatbotlogfile": chatbotLogFile = argValue; return true;
                        case "mcversion": ServerVersion = argValue; return true;
                        case "messagecooldown": messageCooldown = TimeSpan.FromSeconds(str2int(argValue)); return true;
                        case "scriptcache": CacheScripts = str2bool(argValue); return true;
                        case "showsystemmessages": DisplaySystemMessages = str2bool(argValue); return true;
                        case "showxpbarmessages": DisplayXPBarMessages = str2bool(argValue); return true;
                        case "showchatlinks": DisplayChatLinks = str2bool(argValue); return true;
                        case "showinventorylayout": DisplayInventoryLayout = str2bool(argValue); return true;
                        case "terrainandmovements": TerrainAndMovements = str2bool(argValue); return true;
                        case "entityhandling": EntityHandling = str2bool(argValue); return true;
                        case "enableentityhandling": EntityHandling = str2bool(argValue); return true;
                        case "inventoryhandling": InventoryHandling = str2bool(argValue); return true;
                        case "privatemsgscmdname": PrivateMsgsCmdName = argValue.ToLower().Trim(); return true;
                        case "autorespawn": AutoRespawn = str2bool(argValue); return true;
                        // Backward compatible so people can still enable debug with old config format
                        case "debugmessages": DebugMessages = str2bool(argValue); return true;
                        case "minecraftrealms": MinecraftRealmsEnabled = str2bool(argValue); return true;
                        case "moveheadwhilewalking": MoveHeadWhileWalking = str2bool(argValue); return true;

                        case "botowners":
                            Bots_Owners.Clear();
                            string[] names = argValue.ToLower().Split(',');
                            if (!argValue.Contains(",") && argValue.ToLower().EndsWith(".txt") && File.Exists(argValue))
                                names = File.ReadAllLines(argValue);
                            foreach (string name in names)
                                if (!String.IsNullOrWhiteSpace(name))
                                    Bots_Owners.Add(name.Trim());
                            return true;

                        case "internalcmdchar":
                            switch (argValue.ToLower())
                            {
                                case "none": internalCmdChar = ' '; break;
                                case "slash": internalCmdChar = '/'; break;
                                case "backslash": internalCmdChar = '\\'; break;
                            }
                            return true;

                        case "sessioncache":
                            if (argValue == "none") { SessionCaching = CacheType.None; }
                            else if (argValue == "memory") { SessionCaching = CacheType.Memory; }
                            else if (argValue == "disk") { SessionCaching = CacheType.Disk; }
                            return true;

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
                                SetAccount(Login);
                            }
                            return true;

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
                                if (!String.IsNullOrEmpty(ServerAliasTemp))
                                {
                                    SetServerIP(ServerAliasTemp);
                                    ServerAliasTemp = null;
                                }
                            }
                            return true;

                        case "brandinfo":
                            switch (argValue.Trim().ToLower())
                            {
                                case "mcc": BrandInfo = MCCBrandInfo; break;
                                case "vanilla": BrandInfo = "vanilla"; break;
                                default: BrandInfo = null; break;
                            }
                            return true;

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
                            return true;

                        case "mcforge":
                            if (argValue.ToLower() == "auto")
                            {
                                ServerAutodetectForge = true;
                                ServerForceForge = false;
                            }
                            else
                            {
                                ServerAutodetectForge = false;
                                ServerForceForge = str2bool(argValue);
                            }
                            return true;
                    }
                    break;

                case Section.Logging:
                    switch (argName.ToLower())
                    {
                        case "debugmessages": DebugMessages = str2bool(argValue); return true;
                        case "chatmessages": ChatMessages = str2bool(argValue); return true;
                        case "warningmessages": WarningMessages = str2bool(argValue); return true;
                        case "errormessages": ErrorMessages = str2bool(argValue); return true;
                        case "infomessages": InfoMessages = str2bool(argValue); return true;
                        case "chatfilter": ChatFilter = new Regex(argValue); return true;
                        case "debugfilter": DebugFilter = new Regex(argValue); return true;
                        case "filtermode":
                            if (argValue.ToLower().StartsWith("white"))
                                FilterMode = FilterModeEnum.Whitelist;
                            else
                                FilterMode = FilterModeEnum.Blacklist;
                            return true;
                        case "logtofile": LogToFile = str2bool(argValue); return true;
                        case "logfile": LogFile = argValue; return true;
                        case "prependtimestamp": PrependTimestamp = str2bool(argValue); return true;
                        case "savecolorcodes": SaveColorCodes = str2bool(argValue); return true;
                    }
                    break;

                case Section.Alerts:
                    switch (argName.ToLower())
                    {
                        case "enabled": Alerts_Enabled = str2bool(argValue); return true;
                        case "alertsfile": Alerts_MatchesFile = argValue; return true;
                        case "excludesfile": Alerts_ExcludesFile = argValue; return true;
                        case "beeponalert": Alerts_Beep_Enabled = str2bool(argValue); return true;
                    }
                    break;

                case Section.AntiAFK:
                    switch (argName.ToLower())
                    {
                        case "enabled": AntiAFK_Enabled = str2bool(argValue); return true;
                        case "delay": AntiAFK_Delay = str2int(argValue); return true;
                        case "command": AntiAFK_Command = argValue == "" ? "/ping" : argValue; return true;
                    }
                    break;

                case Section.AutoRelog:
                    switch (argName.ToLower())
                    {
                        case "enabled": AutoRelog_Enabled = str2bool(argValue); return true;
                        case "retries": AutoRelog_Retries = str2int(argValue); return true;
                        case "ignorekickmessage": AutoRelog_IgnoreKickMessage = str2bool(argValue); return true;
                        case "kickmessagesfile": AutoRelog_KickMessagesFile = argValue; return true;

                        case "delay":
                            string[] delayParts = argValue.Split('-');
                            if (delayParts.Length == 1)
                            {
                                AutoRelog_Delay_Min = str2int(delayParts[0]);
                                AutoRelog_Delay_Max = AutoRelog_Delay_Min;
                            }
                            else
                            {
                                AutoRelog_Delay_Min = str2int(delayParts[0]);
                                AutoRelog_Delay_Max = str2int(delayParts[1]);
                            }
                            return true;
                    }
                    break;

                case Section.ChatLog:
                    switch (argName.ToLower())
                    {
                        case "enabled": ChatLog_Enabled = str2bool(argValue); return true;
                        case "timestamps": ChatLog_DateTime = str2bool(argValue); return true;
                        case "filter": ChatLog_Filter = ChatBots.ChatLog.str2filter(argValue); return true;
                        case "logfile": ChatLog_File = argValue; return true;
                    }
                    break;

                case Section.Hangman:
                    switch (argName.ToLower())
                    {
                        case "enabled": Hangman_Enabled = str2bool(argValue); return true;
                        case "english": Hangman_English = str2bool(argValue); return true;
                        case "wordsfile": Hangman_FileWords_EN = argValue; return true;
                        case "fichiermots": Hangman_FileWords_FR = argValue; return true;
                    }
                    break;

                case Section.ScriptScheduler:
                    switch (argName.ToLower())
                    {
                        case "enabled": ScriptScheduler_Enabled = str2bool(argValue); return true;
                        case "tasksfile": ScriptScheduler_TasksFile = argValue; return true;
                    }
                    break;

                case Section.RemoteControl:
                    switch (argName.ToLower())
                    {
                        case "enabled": RemoteCtrl_Enabled = str2bool(argValue); return true;
                        case "autotpaccept": RemoteCtrl_AutoTpaccept = str2bool(argValue); return true;
                        case "tpaccepteveryone": RemoteCtrl_AutoTpaccept_Everyone = str2bool(argValue); return true;
                    }
                    break;

                case Section.ChatFormat:
                    switch (argName.ToLower())
                    {
                        case "builtins": ChatFormat_Builtins = str2bool(argValue); return true;
                        case "public": ChatFormat_Public = new Regex(argValue); return true;
                        case "private": ChatFormat_Private = new Regex(argValue); return true;
                        case "tprequest": ChatFormat_TeleportRequest = new Regex(argValue); return true;
                    }
                    break;

                case Section.Proxy:
                    switch (argName.ToLower())
                    {
                        case "enabled":
                            ProxyEnabledLogin = ProxyEnabledIngame = str2bool(argValue);
                            if (argValue.Trim().ToLower() == "login")
                                ProxyEnabledLogin = true;
                            return true;
                        case "type":
                            argValue = argValue.ToLower();
                            if (argValue == "http") { proxyType = Proxy.ProxyHandler.Type.HTTP; }
                            else if (argValue == "socks4") { proxyType = Proxy.ProxyHandler.Type.SOCKS4; }
                            else if (argValue == "socks4a") { proxyType = Proxy.ProxyHandler.Type.SOCKS4a; }
                            else if (argValue == "socks5") { proxyType = Proxy.ProxyHandler.Type.SOCKS5; }
                            return true;
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
                            return true;
                        case "username": ProxyUsername = argValue; return true;
                        case "password": ProxyPassword = argValue; return true;
                    }
                    break;

                case Section.AppVars:
                    SetVar(argName, argValue);
                    return true;

                case Section.AutoRespond:
                    switch (argName.ToLower())
                    {
                        case "enabled": AutoRespond_Enabled = str2bool(argValue); return true;
                        case "matchesfile": AutoRespond_Matches = argValue; return true;
                    }
                    break;

                case Section.AutoAttack:
                    switch (argName.ToLower())
                    {
                        case "enabled": AutoAttack_Enabled = str2bool(argValue); return true;
                        case "mode": AutoAttack_Mode = argValue.ToLower(); return true;
                        case "priority": AutoAttack_Priority = argValue.ToLower(); return true;
                        case "cooldownseconds":
                            if (argValue.ToLower() == "auto")
                            {
                                AutoAttack_OverrideAttackSpeed = false;
                            }
                            else
                            {
                                AutoAttack_OverrideAttackSpeed = true;
                                AutoAttack_CooldownSeconds = str2float(argValue);
                            }
                            return true;
                    }
                    break;

                case Section.AutoFishing:
                    switch (argName.ToLower())
                    {
                        case "enabled": AutoFishing_Enabled = str2bool(argValue); return true;
                        case "antidespawn": AutoFishing_Antidespawn = str2bool(argValue); return true;
                    }
                    break;

                case Section.AutoEat:
                    switch (argName.ToLower())
                    {
                        case "enabled": AutoEat_Enabled = str2bool(argValue); return true;
                        case "threshold": AutoEat_hungerThreshold = str2int(argValue); return true;
                    }
                    break;

                case Section.AutoCraft:
                    switch (argName.ToLower())
                    {
                        case "enabled": AutoCraft_Enabled = str2bool(argValue); return true;
                        case "configfile": AutoCraft_configFile = argValue; return true;
                    }
                    break;

                case Section.AutoDrop:
                    switch (argName.ToLower())
                    {
                        case "enabled": AutoDrop_Enabled = str2bool(argValue); return true;
                        case "mode": AutoDrop_Mode = argValue; return true;
                        case "items": AutoDrop_items = argValue; return true;
                    }
                    break;

                case Section.MCSettings:
                    switch (argName.ToLower())
                    {
                        case "enabled": MCSettings_Enabled = str2bool(argValue); return true;
                        case "locale": MCSettings_Locale = argValue; return true;
                        case "difficulty":
                            switch (argValue.ToLower())
                            {
                                case "peaceful": MCSettings_Difficulty = 0; break;
                                case "easy": MCSettings_Difficulty = 1; break;
                                case "normal": MCSettings_Difficulty = 2; break;
                                case "difficult": MCSettings_Difficulty = 3; break;
                            }
                            return true;
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
                            return true;
                        case "chatmode":
                            switch (argValue.ToLower())
                            {
                                case "enabled": MCSettings_ChatMode = 0; break;
                                case "commands": MCSettings_ChatMode = 1; break;
                                case "disabled": MCSettings_ChatMode = 2; break;
                            }
                            return true;
                        case "chatcolors": MCSettings_ChatColors = str2bool(argValue); return true;
                        case "skin_cape": MCSettings_Skin_Cape = str2bool(argValue); return true;
                        case "skin_jacket": MCSettings_Skin_Jacket = str2bool(argValue); return true;
                        case "skin_sleeve_left": MCSettings_Skin_Sleeve_Left = str2bool(argValue); return true;
                        case "skin_sleeve_right": MCSettings_Skin_Sleeve_Right = str2bool(argValue); return true;
                        case "skin_pants_left": MCSettings_Skin_Pants_Left = str2bool(argValue); return true;
                        case "skin_pants_right": MCSettings_Skin_Pants_Right = str2bool(argValue); return true;
                        case "skin_hat": MCSettings_Skin_Hat = str2bool(argValue); return true;
                        case "main_hand":
                            switch (argValue.ToLower())
                            {
                                case "left": MCSettings_MainHand = 0; break;
                                case "right": MCSettings_MainHand = 1; break;
                            }
                            return true;
                    }
                    break;

                case Section.Mailer:
                    switch (argName.ToLower())
                    {
                        case "enabled": Mailer_Enabled = str2bool(argValue); return true;
                        case "database": Mailer_DatabaseFile = argValue; return true;
                        case "ignorelist": Mailer_IgnoreListFile = argValue; return true;
                        case "publicinteractions": Mailer_PublicInteractions = str2bool(argValue); return true;
                        case "maxmailsperplayer": Mailer_MaxMailsPerPlayer = str2int(argValue); return true;
                        case "maxdatabasesize": Mailer_MaxDatabaseSize = str2int(argValue); return true;
                        case "retentiondays": Mailer_MailRetentionDays = str2int(argValue); return true;
                    }
                    break;

                case Section.ReplayMod:
                    switch (argName.ToLower())
                    {
                        case "enabled": ReplayMod_Enabled = str2bool(argValue); return true;
                        case "backupinterval": ReplayMod_BackupInterval = str2int(argValue); return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// Write an INI file with default settings
        /// </summary>
        /// <param name="settingsfile">File to (over)write</param>
        public static void WriteDefaultSettings(string settingsfile)
        {
            // Load embedded default config and adjust line break for the current operating system
            string settingsContents = String.Join(Environment.NewLine, 
                DefaultConfigResource.MinecraftClient.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));

            // Write configuration file with current version number
            File.WriteAllText(settingsfile,
                "# Minecraft Console Client v"
                + Program.Version
                + Environment.NewLine
                + settingsContents, Encoding.UTF8);
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
                ConsoleIO.WriteLogLine(Translations.Get("error.setting.str2int", str));
                return 0;
            }
        }

        /// <summary>
        /// Convert the specified string to a float number, defaulting to zero if invalid argument
        /// </summary>
        /// <param name="str">String to parse as a float number</param>
        /// <returns>Float number</returns>
        public static float str2float(string str)
        {
            float f;
            if (float.TryParse(str.Trim(), out f))
                return f;
            else
            {
                ConsoleIO.WriteLogLine(Translations.Get("error.setting.str2int", str));
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
        /// Replace %variables% with their value from global AppVars
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="localContext">Optional local variables overriding global variables</param>
        /// <returns>Modifier string</returns>
        public static string ExpandVars(string str, Dictionary<string, object> localVars = null)
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
                        if (!char.IsLetterOrDigit(str[j]) && str[j] != '_')
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
                            case "login": result.Append(Login); break;
                            case "serverip": result.Append(ServerIP); break;
                            case "serverport": result.Append(ServerPort); break;
                            default:
                                if (localVars != null && localVars.ContainsKey(varname_lower))
                                {
                                    result.Append(localVars[varname_lower].ToString());
                                }
                                else if (AppVars.ContainsKey(varname_lower))
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
