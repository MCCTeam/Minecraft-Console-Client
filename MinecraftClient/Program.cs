using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol;
using System.Reflection;

namespace MinecraftClient
{
    /// <summary>
    /// Minecraft Console Client by ORelio (c) 2012-2014.
    /// Allows to connect to any Minecraft server, send and receive text, automated scripts.
    /// This source code is released under the CDDL 1.0 License.
    /// </summary>

    class Program
    {
        private static McTcpClient Client;
        public static string[] startupargs;
        public const string Version = "1.8.0-Indev";

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>

        static void Main(string[] args)
        {
            Console.WriteLine("Console Client for MC 1.4.6 to 1.7.9 - v" + Version + " - By ORelio & Contributors");

            //Basic Input/Output ?
            if (args.Length >= 1 && args[args.Length - 1] == "BasicIO")
            {
                ConsoleIO.basicIO = true;
                Console.OutputEncoding = Console.InputEncoding = Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
                args = args.Where(o => !Object.ReferenceEquals(o, args[args.Length - 1])).ToArray();
            }

            //Process ini configuration file
            if (args.Length >= 1 && System.IO.File.Exists(args[0]) && System.IO.Path.GetExtension(args[0]).ToLower() == ".ini")
            {
                Settings.LoadSettings(args[0]);

                //remove ini configuration file from arguments array
                List<string> args_tmp = args.ToList<string>();
                args_tmp.RemoveAt(0);
                args = args_tmp.ToArray();
            }
            else if (System.IO.File.Exists("MinecraftClient.ini"))
            {
                Settings.LoadSettings("MinecraftClient.ini");
            }
            else Settings.WriteDefaultSettings("MinecraftClient.ini");

            //Other command-line arguments
            if (args.Length >= 1)
            {
                Settings.Login = args[0];
                if (args.Length >= 2)
                {
                    Settings.Password = args[1];
                    if (args.Length >= 3)
                    {
                        Settings.setServerIP(args[2]);

                        //Single command?
                        if (args.Length >= 4)
                        {
                            Settings.SingleCommand = args[3];
                        }
                    }
                }
            }

            if (Settings.ConsoleTitle != "")
            {
                Settings.Username = "New Window";
                Console.Title = Settings.expandVars(Settings.ConsoleTitle);
            }

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
            ProtocolHandler.LoginResult result;
            Settings.Username = Settings.Login;
            string sessionID = "";
            string UUID = "";

            if (Settings.Password == "-")
            {
                ConsoleIO.WriteLineFormatted("§8You chose to run in offline mode.");
                result = ProtocolHandler.LoginResult.Success;
                sessionID = "0";
            }
            else
            {
                Console.WriteLine("Connecting to Minecraft.net...");
                result = ProtocolHandler.GetLogin(ref Settings.Username, Settings.Password, ref sessionID, ref UUID);
            }

            if (result == ProtocolHandler.LoginResult.Success)
            {
                if (Settings.ConsoleTitle != "")
                {
                    Console.Title = Settings.expandVars(Settings.ConsoleTitle);
                }

                Console.WriteLine("Success. (session ID: " + sessionID + ')');
                if (Settings.ServerIP == "")
                {
                    Console.Write("Server IP : ");
                    Settings.setServerIP(Console.ReadLine());
                }

                //Get server version
                int protocolversion = 0;

                if (Settings.ServerVersion != "" && Settings.ServerVersion.ToLower() != "auto")
                {
                    protocolversion = Protocol.ProtocolHandler.MCVer2ProtocolVersion(Settings.ServerVersion);
                    if (protocolversion != 0)
                    {
                        ConsoleIO.WriteLineFormatted("§8Using Minecraft version " + Settings.ServerVersion + " (protocol v" + protocolversion + ')');
                    }
                    else ConsoleIO.WriteLineFormatted("§8Unknown or not supported MC version '" + Settings.ServerVersion + "'.\nSwitching to autodetection mode.");
                }

                if (protocolversion == 0)
                {
                    Console.WriteLine("Retrieving Server Info...");
                    if (!ProtocolHandler.GetServerInfo(Settings.ServerIP, Settings.ServerPort, ref protocolversion))
                    {
                        Console.WriteLine("Failed to ping this IP.");
                        if (Settings.AutoRelog_Enabled)
                        {
                            ChatBots.AutoRelog bot = new ChatBots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries);
                            if (!bot.OnDisconnect(ChatBot.DisconnectReason.ConnectionLost, "Failed to ping this IP.")) { ReadLineReconnect(); }
                        }
                        else ReadLineReconnect();
                        return;
                    }
                }

                if (protocolversion != 0)
                {
                    try
                    {
                        //Start the main TCP client
                        if (Settings.SingleCommand != "")
                        {
                            Client = new McTcpClient(Settings.Username, UUID, sessionID, Settings.ServerIP, Settings.ServerPort, protocolversion, Settings.SingleCommand);
                        }
                        else Client = new McTcpClient(Settings.Username, UUID, sessionID, protocolversion, Settings.ServerIP, Settings.ServerPort);
                    }
                    catch (NotSupportedException)
                    {
                        Console.WriteLine("Cannot connect to the server : This version is not supported !");
                        ReadLineReconnect();
                    }
                }
                else
                {
                    Console.WriteLine("Failed to determine server version.");
                    ReadLineReconnect();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("Connection failed : ");
                switch (result)
                {
                    case ProtocolHandler.LoginResult.AccountMigrated: Console.WriteLine("Account migrated, use e-mail as username."); break;
                    case ProtocolHandler.LoginResult.ServiceUnavailable: Console.WriteLine("Login servers are unavailable. Please try again later."); break;
                    case ProtocolHandler.LoginResult.WrongPassword: Console.WriteLine("Incorrect password."); break;
                    case ProtocolHandler.LoginResult.NotPremium: Console.WriteLine("User not premium."); break;
                    case ProtocolHandler.LoginResult.OtherError: Console.WriteLine("Network error."); break;
                    case ProtocolHandler.LoginResult.SSLError: Console.WriteLine("SSL Error.");
                        if (isUsingMono)
                        {
                            ConsoleIO.WriteLineFormatted("§8It appears that you are using Mono to run this program."
                                + '\n' + "The first time, you have to import HTTPS certificates using:"
                                + '\n' + "mozroots --import --ask-remove");
                            return;
                        }
                        break;
                }
                while (Console.KeyAvailable) { Console.ReadKey(false); }
                if (Settings.SingleCommand == "") { ReadLineReconnect(); }
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
            if (!Settings.exitOnFailure)
            {
                string text = Console.ReadLine();
                if (text == "reco" || text == "reconnect" || text == "/reco" || text == "/reconnect")
                {
                    Program.Restart();
                    return true;
                }
                else return false;
            }
            else return false;
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
    }
}
