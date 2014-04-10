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
        public static string SingleCommand = "";
        public static string ConsoleTitle = "";

        //Other Settings
        public static string TranslationsFile_FromMCDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\.minecraft\assets\objects\9e\9e2fdc43fc1c7024ff5922b998fadb2971a64ee0"; //MC 1.7.4 en_GB.lang
        public static string TranslationsFile_Website_Index = "https://s3.amazonaws.com/Minecraft.Download/indexes/1.7.4.json";
        public static string TranslationsFile_Website_Download = "http://resources.download.minecraft.net";
        public static List<string> Bots_Owners = new List<string>();
        public static string Language = "en_GB";
        public static bool chatTimeStamps = false;

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
        public static string Alerts_MatchesFile = "alerts.txt";
        public static string Alerts_ExcludesFile = "alerts-exclude.txt";

        //ChatLog Settings
        public static bool ChatLog_Enabled = false;
        public static bool ChatLog_DateTime = true;
        public static string ChatLog_File = "chatlog.txt";
        public static Bots.ChatLog.MessageFilter ChatLog_Filter = Bots.ChatLog.MessageFilter.AllMessages;

        //PlayerListLog Settings
        public static bool PlayerLog_Enabled = false;
        public static string PlayerLog_File = "playerlog.txt";
        public static int PlayerLog_Delay = 600;

        //AutoRelog Settings
        public static bool AutoRelog_Enabled = false;
        public static int AutoRelog_Delay = 10;
        public static int AutoRelog_Retries = 3;
        public static string AutoRelog_KickMessagesFile = "kickmessages.txt";

        //Startup Script Settings
        public static bool StartupScript_Enabled = false;
        public static string StartupScript_ScriptFile = "script.txt";

        //Remote Control
        public static bool RemoteCtrl_Enabled = false;

        private enum ParseMode { Default, Main, AntiAFK, Hangman, Alerts, ChatLog, AutoRelog, Scripting, RemoteControl };

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
                                    case "startupscript": pMode = ParseMode.Scripting; break;
                                    case "remotecontrol": pMode = ParseMode.RemoteControl; break;
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
                                                case "serverip": ServerIP = argValue; break;
                                                case "singlecommand": SingleCommand = argValue; break;
                                                case "language": Language = argValue; break;
                                                case "consoletitle": ConsoleTitle = argValue; break;
                                                case "timestamps": chatTimeStamps = str2bool(argValue); break;
                                                case "botowners":
                                                    Bots_Owners.Clear();
                                                    foreach (string name in argValue.ToLower().Replace(" ", "").Split(','))
                                                        Bots_Owners.Add(name);
                                                    break;
                                            }
                                            break;

                                        case ParseMode.Alerts:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": Alerts_Enabled = str2bool(argValue); break;
                                                case "alertsfile": Alerts_MatchesFile = argValue; break;
                                                case "excludesfile": Alerts_ExcludesFile = argValue; break;
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
                                                case "filter": ChatLog_Filter = Bots.ChatLog.str2filter(argValue); break;
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

                                        case ParseMode.Scripting:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": StartupScript_Enabled = str2bool(argValue); break;
                                                case "scriptfile": StartupScript_ScriptFile = argValue; break;
                                            }
                                            break;

                                        case ParseMode.RemoteControl:
                                            switch (argName.ToLower())
                                            {
                                                case "enabled": RemoteCtrl_Enabled = str2bool(argValue); break;
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
            System.IO.File.WriteAllText(settingsfile, "#Minecraft Console Client v" + Program.Version + "\r\n"
                + "#Startup Config File\r\n"
                + "\r\n"
                + "[Main]\r\n"
                + "\r\n"
                + "#General settings\r\n"
                + "#leave blank to prompt user on startup\r\n"
                + "#Use \"-\" as password for offline mode\r\n"
                + "\r\n"
                + "login=\r\npassword=\r\nserverip=\r\n"
                + "\r\n"
                + "#Advanced settings\r\n"
                + "\r\n"
                + "language=en_GB\r\n"
                + "botowners=Player1,Player2,Player3\r\n"
                + "consoletitle=%username% - Minecraft Console Client\r\n"
                + "timestamps=false\r\n"
                + "\r\n"
                + "#Bot Settings\r\n"
                + "\r\n"
                + "[Alerts]\r\n"
                + "enabled=false\r\n"
                + "alertsfile=alerts.txt\r\n"
                + "excludesfile=alerts-exclude.txt\r\n"
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
                + "logfile=chatlog.txt\r\n"
                + "\r\n"
                + "[Hangman]\r\n"
                + "enabled=false\r\n"
                + "english=true\r\n"
                + "wordsfile=hangman-en.txt\r\n"
                + "fichiermots=hangman-fr.txt\r\n"
                + "\r\n"
                + "[StartupScript]\r\n"
                + "enabled=false\r\n"
                + "scriptfile=testscript.txt\r\n"
                + "\r\n"
                + "[RemoteControl]\r\n"
                + "enabled=false\r\n", Encoding.UTF8);
        }

        public static int str2int(string str) { try { return Convert.ToInt32(str); } catch { return 0; } }
        public static bool str2bool(string str) { return str == "true" || str == "1"; }

    }
}
