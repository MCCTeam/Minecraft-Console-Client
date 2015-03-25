using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MinecraftClient
{
    /// <summary>
    /// Contains main settings for Minecraft Console Client
    /// Allows settings loading from an INI file
    /// </summary>

    public static class Settings
    {
        //Main Settings.
        //Login: Username or email adress used as login for Minecraft/Mojang account
        //Username: The actual username of the user, obtained after login to the account
        public static string Login = "";
        public static string Username = "";
        public static string Password = "";
        public static string ServerIP = "";
        public static ushort ServerPort = 25565;
        public static string ServerVersion  = "";
        public static string SingleCommand = "";
        public static string ConsoleTitle = "";

        //Proxy Settings
        public static bool ProxyEnabled = false;
        public static string ProxyHost = "";
        public static int ProxyPort = 0;
        public static Proxy.ProxyHandler.Type proxyType = Proxy.ProxyHandler.Type.HTTP;
        public static string ProxyUsername = "";
        public static string ProxyPassword = "";

        //Other Settings
        public static string TranslationsFile_FromMCDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\.minecraft\assets\objects\9e\9e2fdc43fc1c7024ff5922b998fadb2971a64ee0"; //MC 1.7.4 en_GB.lang
        public static string TranslationsFile_Website_Index = "https://s3.amazonaws.com/Minecraft.Download/indexes/1.7.4.json";
        public static string TranslationsFile_Website_Download = "http://resources.download.minecraft.net";
        public static List<string> Bots_Owners = new List<string>();
        public static string Language = "en_GB";
        public static bool chatTimeStamps = false;
        public static bool interactiveMode = true;
        public static char internalCmdChar = '/';
        public static bool playerHeadAsIcon = false;
        public static string chatbotLogFile = "";

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

        //Custom app variables and Minecraft accounts
        private static Dictionary<string, string> AppVars = new Dictionary<string, string>();
        private static Dictionary<string, KeyValuePair<string, string>> Accounts = new Dictionary<string, KeyValuePair<string, string>>();
        private static Dictionary<string, KeyValuePair<string, ushort>> Servers = new Dictionary<string, KeyValuePair<string, ushort>>();

        private enum ParseMode { Default, Main, AppVars, Proxy, AntiAFK, Hangman, Alerts, ChatLog, AutoRelog, ScriptScheduler, RemoteControl };

        /// <summary>
        /// Load settings from the give INI file
        /// </summary>
        /// <param name="settingsfile">File to load</param>

        public static void LoadSettings(string settingsfile)
        {
            if (File.Exists(settingsfile))
            {
                try
                {
                    string[] Lines = File.ReadAllLines(settingsfile);
                    ParseMode pMode = ParseMode.Default;
                    foreach (string lineRAW in Lines)
                    {
                        string line = lineRAW.Split('#')[0].Trim();
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
                                    case "scriptscheduler": pMode = ParseMode.ScriptScheduler; break;
                                    case "remotecontrol": pMode = ParseMode.RemoteControl; break;
                                    case "proxy": pMode = ParseMode.Proxy; break;
                                    case "appvars": pMode = ParseMode.AppVars; break;
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
                                                case "serverip": setServerIP(argValue); break;
                                                case "singlecommand": SingleCommand = argValue; break;
                                                case "language": Language = argValue; break;
                                                case "consoletitle": ConsoleTitle = argValue; break;
                                                case "timestamps": chatTimeStamps = str2bool(argValue); break;
                                                case "exitonfailure": interactiveMode = !str2bool(argValue); break;
                                                case "playerheadicon": playerHeadAsIcon = str2bool(argValue); break;
                                                case "chatbotlogfile": chatbotLogFile = argValue; break;
                                                case "mcversion": ServerVersion = argValue; break;

                                                case "botowners":
                                                    Bots_Owners.Clear();
                                                    foreach (string name in argValue.ToLower().Split(','))
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
                                                                && setServerIP(server_data[1]))
                                                                Servers[server_data[0]]
                                                                    = new KeyValuePair<string, ushort>(ServerIP, ServerPort);
                                                        }
                                                        
                                                        //Restore current server info
                                                        ServerIP = server_host_temp;
                                                        ServerPort = server_port_temp;
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

                                        case ParseMode.Proxy:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": ProxyEnabled = str2bool(argValue); break;
                                                case "type":
                                                    argValue = argValue.ToLower();
                                                    if (argValue == "http") { proxyType = Proxy.ProxyHandler.Type.HTTP; }
                                                    else if (argValue == "socks4") { proxyType = Proxy.ProxyHandler.Type.SOCKS4; }
                                                    else if (argValue == "socks4a"){ proxyType = Proxy.ProxyHandler.Type.SOCKS4a;}
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
                                            setVar(argName, argValue);
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
            System.IO.File.WriteAllText(settingsfile, "#Minecraft Console Client v" + Program.Version + "\r\n"
                + "#Startup Config File\r\n"
                + "\r\n"
                + "[Main]\r\n"
                + "\r\n"
                + "#General settings\r\n"
                + "#leave blank to prompt user on startup\r\n"
                + "#Use \"-\" as password for offline mode\r\n"
                + "\r\n"
                + "login=\r\n"
                + "password=\r\n"
                + "serverip=\r\n"
                + "\r\n"
                + "#Advanced settings\r\n"
                + "\r\n"
                + "language=en_GB\r\n"
                + "botowners=Player1,Player2,Player3\r\n"
                + "consoletitle=%username% - Minecraft Console Client\r\n"
                + "internalcmdchar=slash #use 'none', 'slash' or 'backslash'\r\n"
                + "mcversion=auto #use 'auto' or '1.X.X' values\r\n"
                + "chatbotlogfile= #leave empty for no logfile\r\n"
                + "accountlist=accounts.txt\r\n"
                + "serverlist=servers.txt\r\n"
                + "playerheadicon=true\r\n"
                + "exitonfailure=false\r\n"
                + "timestamps=false\r\n"
                + "\r\n"
                + "[AppVars]\r\n"
                + "#yourvar=yourvalue\r\n"
                + "#can be used in some other fields as %yourvar%\r\n"
                + "#%username% and %serverip% are reserved variables.\r\n"
                + "\r\n"
                + "[Proxy]\r\n"
                + "enabled=false\r\n"
                + "type=HTTP #Supported types: HTTP, SOCKS4, SOCKS4a, SOCKS5\r\n"
                + "server=0.0.0.0:0000\r\n"
                + "username=\r\n"
                + "password=\r\n"
                + "\r\n"
                + "#Bot Settings\r\n"
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
                + "tpaccepteveryone=false\r\n", Encoding.UTF8);
        }

        public static int str2int(string str) { try { return Convert.ToInt32(str); } catch { return 0; } }
        public static bool str2bool(string str) { return str == "true" || str == "1"; }

        /// <summary>
        /// Load login/password using an account alias
        /// </summary>
        /// <returns>True if the account was found and loaded</returns>

        public static bool setAccount(string accountAlias)
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

        public static bool setServerIP(string server)
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
                ServerIP = host;
                ServerPort = port;
                return true;
            }
            else if (Servers.ContainsKey(server))
            {
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

        public static bool setVar(string varName, string varData)
        {
            varName = new string(varName.TakeWhile(char.IsLetterOrDigit).ToArray()).ToLower();
            if (varName.Length > 0)
            {
                AppVars[varName] = varData;
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Replace %variables% with their value
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <returns>Modifier string</returns>

        public static string expandVars(string str)
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
                                    result.Append(AppVars[varname_lower]);
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
