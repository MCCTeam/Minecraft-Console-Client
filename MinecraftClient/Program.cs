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
        private static string loginusername = "";
        private static string user = "";
        private static string pass = "";
        private static string ip = "";
        private static string command = "";
        private static string[] startupargs;

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>

        static void Main(string[] args)
        {
            Console.WriteLine("Console Client for MC 1.4.6 to 1.6.2 - v1.5.2 - By ORelio (or3L1o@live.fr)");

            //Processing Command-line arguments

            if (args.Length >= 1)
            {
                user = args[0];
                if (args.Length >= 2)
                {
                    pass = args[1];
                    if (args.Length >= 3)
                    {
                        ip = args[2];
                        if (args.Length >= 4)
                        {
                            command = args[3];
                        }
                    }
                }
            }

            //Asking the user to type in missing data such as Username and Password

            if (user == "")
            {
                Console.Write("Username : ");
                user = Console.ReadLine();
            }
            if (pass == "")
            {
                Console.Write("Password : ");
                pass = Console.ReadLine();

                //Hide the password
                Console.CursorTop--;
                Console.Write("Password : <******>");
                for (int i = 19; i < Console.BufferWidth; i++) { Console.Write(' '); }
            }

            //Save the arguments
            startupargs = args;
            loginusername = user;

            //Start the Client
            InitializeClient();
        }

        /// <summary>
        /// Start a new Client
        /// </summary>

        private static void InitializeClient()
        {

            MinecraftCom.LoginResult result;
            string logindata = "";

            if (pass == "-")
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("You chose to run in offline mode.");
                Console.ForegroundColor = ConsoleColor.Gray;
                result = MinecraftCom.LoginResult.Success;
                logindata = "0:deprecated:" + user + ":0";
            }
            else
            {
                Console.WriteLine("Connecting to Minecraft.net...");
                result = MinecraftCom.GetLogin(loginusername, pass, ref logindata);
            }
            if (result == MinecraftCom.LoginResult.Success)
            {
                user = logindata.Split(':')[2];
                string sessionID = logindata.Split(':')[3];
                Console.WriteLine("Success. (session ID: " + sessionID + ')');
                if (ip == "")
                {
                    Console.Write("Server IP : ");
                    ip = Console.ReadLine();
                }

                //Get server version
                Console.WriteLine("Retrieving Server Info...");
                byte protocolversion = 0; string version = "";
                if (MinecraftCom.GetServerInfo(ip, ref protocolversion, ref version))
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
                        foreach (string arg in startupargs)
                        {
                            if (arg.Length > 4 && arg.Substring(0, 4).ToLower() == "bot:")
                            {
                                int param;
                                string[] botargs = arg.ToLower().Split(':');
                                switch (botargs[1])
                                {
                                    case "antiafk":
                                        #region Arguments for the AntiAFK bot
                                        param = 600;
                                        if (botargs.Length > 2)
                                        {
                                            try { param = Convert.ToInt32(botargs[2]); }
                                            catch (FormatException) { }
                                        }
                                        #endregion
                                        handler.BotLoad(new Bots.AntiAFK(param)); break;

                                    case "pendu": handler.BotLoad(new Bots.Pendu(false)); break;
                                    case "hangman": handler.BotLoad(new Bots.Pendu(true)); break;
                                    case "alerts": handler.BotLoad(new Bots.Alerts()); break;

                                    case "log":
                                        #region Arguments for the ChatLog bot
                                        bool datetime = true;
                                        string file = "chat-" + ip + ".log";
                                        Bots.ChatLog.MessageFilter filter = Bots.ChatLog.MessageFilter.AllMessages;
                                        if (botargs.Length > 2)
                                        {
                                            datetime = (botargs[2] != "0");
                                            if (botargs.Length > 3)
                                            {
                                                switch (botargs[3])
                                                {
                                                    case "all": filter = Bots.ChatLog.MessageFilter.AllText; break;
                                                    case "messages": filter = Bots.ChatLog.MessageFilter.AllMessages; break;
                                                    case "chat": filter = Bots.ChatLog.MessageFilter.OnlyChat; break;
                                                    case "private": filter = Bots.ChatLog.MessageFilter.OnlyWhispers; break;
                                                }
                                                if (botargs.Length > 4 && botargs[4] != "") { file = botargs[4]; }
                                            }
                                        }
                                        #endregion
                                        handler.BotLoad(new Bots.ChatLog(file, filter, datetime)); break;

                                    case "logplayerlist":
                                        #region Arguments for the PlayerListLogger bot
                                        param = 600;
                                        if (botargs.Length > 2)
                                        {
                                            try { param = Convert.ToInt32(botargs[2]); }
                                            catch (FormatException) { }
                                        }
                                        #endregion
                                        handler.BotLoad(new Bots.PlayerListLogger(param, "connected-" + ip + ".log")); break;

                                    case "autorelog":
                                        #region Arguments for the AutoRelog bot
                                        int delay = 10;
                                        if (botargs.Length > 2)
                                        {
                                            try { delay = Convert.ToInt32(botargs[2]); }
                                            catch (FormatException) { }
                                        }
                                        int retries = 3;
                                        if (botargs.Length > 3)
                                        {
                                            try { retries = Convert.ToInt32(botargs[3]); }
                                            catch (FormatException) { }
                                        }
                                        #endregion
                                        handler.BotLoad(new Bots.AutoRelog(delay, retries)); break;

                                    case "xauth": if (botargs.Length > 2) { handler.BotLoad(new Bots.xAuth(botargs[2])); } break;
                                    case "scripting": if (botargs.Length > 2) { handler.BotLoad(new Bots.Scripting(botargs[2])); } break;
                                }
                                command = "";
                            }
                        }
                        //Start the main TCP client
                        if (command != "")
                        {
                            Client = new McTcpClient(user, sessionID, ip, handler, command);
                        }
                        else Client = new McTcpClient(user, sessionID, ip, handler);
                    }
                    else
                    {
                        Console.WriteLine("Cannot connect to the server : This version is not supported !");
                        ReadLineReconnect();
                    }
                }
                else
                {
                    Console.WriteLine("Failed to ping this IP.");
                    ReadLineReconnect();
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
                    case MinecraftCom.LoginResult.WrongPassword: Console.WriteLine("Incorrect password."); break;
                    case MinecraftCom.LoginResult.NotPremium: Console.WriteLine("User not premium."); break;
                    case MinecraftCom.LoginResult.Error: Console.WriteLine("Network error."); break;
                }
                while (Console.KeyAvailable) { Console.ReadKey(false); }
                if (command == "") { ReadLineReconnect(); }
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
