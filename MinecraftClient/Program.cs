using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient
{
    /// <summary>
    /// Minecraft Console Client by ORelio (c) 2012-2013.
    /// Allows to connect to any Minecraft server, send and receive text, automated scripts.
    /// This source code is released under the CDDL 1.0 License.
    /// </summary>

    class Program
    {
        private static McTcpClient Client;
        public static string[] startupargs;
        public const string Version = "1.6.0";

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>

        static void Main(string[] args)
        {
            Console.WriteLine("Console Client for MC 1.4.6 to 1.6.2 - v" + Version + " - By ORelio (or3L1o@live.fr)");

            //Basic Input/Output ?
            if (args.Length >= 1 && args[args.Length - 1] == "BasicIO")
            {
                ConsoleIO.basicIO = true;
                args = args.Where(o => !Object.ReferenceEquals(o, args[args.Length - 1])).ToArray();
            }

            //Processing Command-line arguments or Config File

            if (args.Length == 1 && System.IO.File.Exists(args[0]))
            {
                Settings.LoadSettings(args[0]);
            }
            else if (args.Length >= 1)
            {
                Settings.Login = args[0];
                if (args.Length >= 2)
                {
                    Settings.Password = args[1];
                    if (args.Length >= 3)
                    {
                        Settings.ServerIP = args[2];

                        //Single command?
                        if (args.Length >= 4)
                        {
                            Settings.SingleCommand = args[3];
                        }

                        //Use bots? (will disable single command)
                        for (int i = 3; i < args.Length; i++)
                        {
                            if (args[i].Length > 4 && args[i].Substring(0, 4).ToLower() == "bot:")
                            {
                                Settings.SingleCommand = "";
                                string[] botargs = args[i].ToLower().Split(':');
                                switch (botargs[1])
                                {
                                    #region Process bots settings
                                    case "antiafk":
                                        Settings.AntiAFK_Enabled = true;
                                        if (botargs.Length > 2)
                                        {
                                            try { Settings.AntiAFK_Delay = Convert.ToInt32(botargs[2]); }
                                            catch (FormatException) { }
                                        } break;

                                    case "pendu":
                                        Settings.Hangman_Enabled = true;
                                        Settings.Hangman_English = false;
                                        break;

                                    case "hangman":
                                        Settings.Hangman_Enabled = true;
                                        Settings.Hangman_English = true;
                                        break;

                                    case "alerts":
                                        Settings.Alerts_Enabled = true;
                                        break;

                                    case "log":
                                        Settings.ChatLog_Enabled = true;
                                        Settings.ChatLog_DateTime = true;
                                        Settings.ChatLog_File = "chat-" + Settings.ServerIP.Replace(':', '-') + ".log";
                                        if (botargs.Length > 2)
                                        {
                                            Settings.ChatLog_DateTime = Settings.str2bool(botargs[2]);
                                            if (botargs.Length > 3)
                                            {
                                                Settings.ChatLog_Filter = Bots.ChatLog.str2filter(botargs[3]);
                                                if (botargs.Length > 4 && botargs[4] != "") { Settings.ChatLog_File = botargs[4]; }
                                            }
                                        } break;

                                    case "logplayerlist":
                                        Settings.PlayerLog_File = "connected-" + Settings.ServerIP.Replace(':', '-') + ".log";
                                        if (botargs.Length > 2)
                                        {
                                            try { Settings.PlayerLog_Delay = Convert.ToInt32(botargs[2]); }
                                            catch (FormatException) { }
                                        } break;

                                    case "autorelog":
                                        if (botargs.Length > 2)
                                        {
                                            try { Settings.AutoRelog_Delay = Convert.ToInt32(botargs[2]); }
                                            catch (FormatException) { }
                                            if (botargs.Length > 3)
                                            {
                                                try { Settings.AutoRelog_Retries = Convert.ToInt32(botargs[3]); }
                                                catch (FormatException) { }
                                            }
                                        } break;

                                    case "xauth":
                                        if (botargs.Length > 2)
                                        {
                                            Settings.xAuth_Enabled = true;
                                            Settings.xAuth_Password = botargs[2];
                                        } break;

                                    case "scripting":
                                        if (botargs.Length > 2)
                                        {
                                            Settings.Scripting_Enabled = true;
                                            Settings.Scripting_ScriptFile = botargs[2];
                                        } break;

                                    #endregion
                                }
                            }
                        }
                    }
                }
            }
            else if (System.IO.File.Exists("MinecraftClient.ini"))
            {
                Settings.LoadSettings("MinecraftClient.ini");
            }
            else Settings.WriteDefaultSettings("MinecraftClient.ini");

            //Asking the user to type in missing data such as Username and Password

            if (Settings.Login == "")
            {
                Console.Write(ConsoleIO.basicIO ? "Please type the username of your choice.\n" : "Username : ");
                Settings.Login = Console.ReadLine();
            }
            if (Settings.Password == "")
            {
                Console.Write(ConsoleIO.basicIO ? "Please type the password for " + Settings.Login + ".\n" : "Password : ");
                Settings.Password = ConsoleIO.basicIO ? Console.ReadLine() : ConsoleIO.ReadPassword();
                if (Settings.Password == "") { Settings.Password = "-"; }
                if (!ConsoleIO.basicIO)
                {
                    //Hide password length
                    Console.CursorTop--; Console.Write("Password : <******>");
                    for (int i = 19; i < Console.BufferWidth; i++) { Console.Write(' '); }
                }
            }

            startupargs = args;
            InitializeClient();
        }

        /// <summary>
        /// Start a new Client
        /// </summary>

        private static void InitializeClient()
        {

            MinecraftCom.LoginResult result;
            string logindata = "";

            if (Settings.Password == "-")
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("You chose to run in offline mode.");
                Console.ForegroundColor = ConsoleColor.Gray;
                result = MinecraftCom.LoginResult.Success;
                logindata = "0:deprecated:" + Settings.Login + ":0";
            }
            else
            {
                Console.WriteLine("Connecting to Minecraft.net...");
                result = MinecraftCom.GetLogin(Settings.Login, Settings.Password, ref logindata);
            }
            if (result == MinecraftCom.LoginResult.Success)
            {
                Settings.Username = logindata.Split(':')[2];
                string sessionID = logindata.Split(':')[3];
                Console.WriteLine("Success. (session ID: " + sessionID + ')');
                if (Settings.ServerIP == "")
                {
                    Console.Write("Server IP : ");
                    Settings.ServerIP = Console.ReadLine();
                }

                //Get server version
                Console.WriteLine("Retrieving Server Info...");
                byte protocolversion = 0; string version = "";
                if (MinecraftCom.GetServerInfo(Settings.ServerIP, ref protocolversion, ref version))
                {
                    //Supported protocol version ?
                    int[] supportedVersions = { 51, 60, 61, 72, 73, 74 };
                    if (Array.IndexOf(supportedVersions, protocolversion) > -1)
                    {
                        //Minecraft 1.6+ ? Load translations
                        if (protocolversion >= 72) { ChatParser.InitTranslations(); }

                        //Will handle the connection for this client
                        Console.WriteLine("Version is supported.");
                        MinecraftCom handler = new MinecraftCom();
                        ConsoleIO.SetAutoCompleteEngine(handler);
                        handler.setVersion(protocolversion);

                        //Load & initialize bots if needed
                        if (Settings.AntiAFK_Enabled)   { handler.BotLoad(new Bots.AntiAFK(Settings.AntiAFK_Delay)); }
                        if (Settings.Hangman_Enabled)   { handler.BotLoad(new Bots.Pendu(Settings.Hangman_English)); }
                        if (Settings.Alerts_Enabled)    { handler.BotLoad(new Bots.Alerts()); }
                        if (Settings.ChatLog_Enabled)   { handler.BotLoad(new Bots.ChatLog(Settings.ChatLog_File, Settings.ChatLog_Filter, Settings.ChatLog_DateTime)); }
                        if (Settings.PlayerLog_Enabled) { handler.BotLoad(new Bots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.PlayerLog_File)); }
                        if (Settings.AutoRelog_Enabled) { handler.BotLoad(new Bots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries)); }
                        if (Settings.xAuth_Enabled)     { handler.BotLoad(new Bots.xAuth(Settings.xAuth_Password)); }
                        if (Settings.Scripting_Enabled) { handler.BotLoad(new Bots.Scripting(Settings.Scripting_ScriptFile)); }

                        //Start the main TCP client
                        if (Settings.SingleCommand != "")
                        {
                            Client = new McTcpClient(Settings.Username, sessionID, Settings.ServerIP, handler, Settings.SingleCommand);
                        }
                        else Client = new McTcpClient(Settings.Username, sessionID, Settings.ServerIP, handler);
                    }
                    else
                    {
                        Console.WriteLine("Cannot connect to the server : This version is not supported !");
                        ReadLineReconnect();
                        Console.WriteLine("Waiting " + Settings.Retry_Delay + " seconds before reconnecting...");
                        System.Threading.Thread.Sleep(Settings.Retry_Delay * 1000); Restart();
                    }
                }
                else
                {
                    Console.WriteLine("Failed to ping this IP.");
                    Console.WriteLine("Waiting " + Settings.Retry_Delay + " seconds before reconnecting...");
                    System.Threading.Thread.Sleep(Settings.Retry_Delay * 1000); Restart();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("Connection failed : ");
                switch (result)
                {
                    case MinecraftCom.LoginResult.AccountMigrated: Console.WriteLine("Account migrated, use e-mail as username."); break;
                    case MinecraftCom.LoginResult.Blocked: Console.WriteLine("Too many failed logins. Please try again later."); break;
                    case MinecraftCom.LoginResult.BadRequest: Console.WriteLine("Login attempt rejected: Bad request."); break;
                    case MinecraftCom.LoginResult.WrongPassword: Console.WriteLine("Incorrect password."); break;
                    case MinecraftCom.LoginResult.NotPremium: Console.WriteLine("User not premium."); break;
                    case MinecraftCom.LoginResult.Error: Console.WriteLine("Network error."); break;
                }
                while (Console.KeyAvailable) { Console.ReadKey(false); }
                if (Settings.SingleCommand == "") { Console.WriteLine("Waiting " + Settings.Retry_Delay + " seconds before reconnecting..."); }
                {System.Threading.Thread.Sleep(Settings.Retry_Delay * 1000); Restart();
                }
            }
        }

        /// <summary>
        /// Disconnect the current client from the server and restart it
        /// </summary>

        public static void Restart()
        {
            new System.Threading.Thread(new System.Threading.ThreadStart(t_restart)).Start();
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>

        public static void Exit()
        {
            new System.Threading.Thread(new System.Threading.ThreadStart(t_exit)).Start();
        }

        /// <summary>
        /// Pause the program, usually when an error or a kick occured, letting the user press Enter to quit OR type /reconnect
        /// </summary>
        /// <returns>Return True if the user typed "/reconnect"</returns>

        public static bool ReadLineReconnect()
        {
            string text = Console.ReadLine();
            if (text == "reco" || text == "reconnect" || text == "/reco" || text == "/reconnect")
            {
                Program.Restart();
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Private thread for restarting the program. Called through Restart()
        /// </summary>

        private static void t_restart()
        {
            if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
            Console.WriteLine("Restarting Minecraft Console Client...");
            InitializeClient();
        }

        /// <summary>
        /// Private thread for exiting the program. Called through Exit()
        /// </summary>

        private static void t_exit()
        {
            if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
            Environment.Exit(0);
        }
    }
}
