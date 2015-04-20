using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol;
using System.Reflection;
using System.Threading;

namespace MinecraftClient
{
    /// <summary>
    /// Minecraft Console Client by ORelio (c) 2012-2014.
    /// Allows to connect to any Minecraft server, send and receive text, automated scripts.
    /// This source code is released under the CDDL 1.0 License.
    /// </summary>

    static class Program
    {
        private static McTcpClient Client;
        public static string[] startupargs;
        public const string Version = "1.8.2";
        private static Thread offlinePrompt = null;
        private static bool useMcVersionOnce = false;

        /// <summary>
        /// The main entry point of Minecraft Console Client
        /// </summary>

        static void Main(string[] args)
        {
            Console.WriteLine("Console Client for MC 1.4.6 to 1.8.3 - v" + Version + " - By ORelio & Contributors");

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
                    Console.Title = Settings.expandVars(Settings.ConsoleTitle);
                
                if (Settings.playerHeadAsIcon)
                    ConsoleIcon.setPlayerIconAsync(Settings.Username);
                
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

                    if (useMcVersionOnce)
                    {
                        useMcVersionOnce = false;
                        Settings.ServerVersion = "";
                    }
                }

                if (protocolversion == 0)
                {
                    Console.WriteLine("Retrieving Server Info...");
                    if (!ProtocolHandler.GetServerInfo(Settings.ServerIP, Settings.ServerPort, ref protocolversion))
                    {
                        Console.WriteLine("Failed to ping this IP.");
                        if (!ChatBots.AutoRelog.OnDisconnectStatic(ChatBot.DisconnectReason.ConnectionLost, "Failed to ping this IP."))
                            HandleServerVersionFailure();
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
                        HandleServerVersionFailure();
                    }
                }
                else
                {
                    Console.WriteLine("Failed to determine server version.");
                    HandleServerVersionFailure();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                string failureMessage = "Minecraft Login failed : ";
                switch (result)
                {
                    case ProtocolHandler.LoginResult.AccountMigrated: failureMessage += "Account migrated, use e-mail as username."; break;
                    case ProtocolHandler.LoginResult.ServiceUnavailable: failureMessage += "Login servers are unavailable. Please try again later."; break;
                    case ProtocolHandler.LoginResult.WrongPassword: failureMessage += "Incorrect password."; break;
                    case ProtocolHandler.LoginResult.NotPremium: failureMessage += "User not premium."; break;
                    case ProtocolHandler.LoginResult.OtherError: failureMessage += "Network error."; break;
                    case ProtocolHandler.LoginResult.SSLError: failureMessage += "SSL Error."; break;
                    default: failureMessage += "Unknown Error."; break;
                }
                Console.WriteLine(failureMessage);
                if (result == ProtocolHandler.LoginResult.SSLError && isUsingMono)
                {
                    ConsoleIO.WriteLineFormatted("§8It appears that you are using Mono to run this program."
                        + '\n' + "The first time, you have to import HTTPS certificates using:"
                        + '\n' + "mozroots --import --ask-remove");
                    return;
                }
                while (Console.KeyAvailable) { Console.ReadKey(false); }
                if (!ChatBots.AutoRelog.OnDisconnectStatic(ChatBot.DisconnectReason.LoginRejected, failureMessage))
                    HandleOfflineMode();
            }
        }

        /// <summary>
        /// Disconnect the current client from the server and restart it
        /// </summary>

        public static void Restart()
        {
            new Thread(new ThreadStart(delegate
            {
                if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Abort(); offlinePrompt = null; ConsoleIO.Reset(); }
                Console.WriteLine("Restarting Minecraft Console Client...");
                InitializeClient();
            })).Start();
        }

        /// <summary>
        /// Disconnect the current client from the server and exit the app
        /// </summary>

        public static void Exit()
        {
            new Thread(new ThreadStart(delegate
            {
                if (Client != null) { Client.Disconnect(); ConsoleIO.Reset(); }
                if (offlinePrompt != null) { offlinePrompt.Abort(); offlinePrompt = null; ConsoleIO.Reset(); }
                if (Settings.playerHeadAsIcon) { ConsoleIcon.revertToCMDIcon(); }
                Environment.Exit(0);
            })).Start();
        }

        /// <summary>
        /// Pause the program, usually when an error or a kick occured, letting the user typing commands to reconnect to a server
        /// </summary>

        public static void HandleOfflineMode()
        {
            if (Settings.interactiveMode && offlinePrompt == null)
            {
                offlinePrompt = new Thread(new ThreadStart(delegate
                {
                    string command = " ";
                    ConsoleIO.WriteLineFormatted("Not connected to any server. Use '" + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + "help' for help.");
                    ConsoleIO.WriteLineFormatted("Or press Enter to exit Minecraft Console Client.");
                    while (command.Length > 0)
                    {
                        if (!ConsoleIO.basicIO) { ConsoleIO.Write('>'); }
                        command = Console.ReadLine().Trim();
                        if (command.Length > 0)
                        {
                            string message = "";

                            if (Settings.internalCmdChar != ' '
                                && command[0] == Settings.internalCmdChar)
                                command = command.Substring(1);
                            
                            if (command.StartsWith("reco"))
                            {
                                message = new Commands.Reco().Run(null, Settings.expandVars(command));
                            }
                            else if (command.StartsWith("connect"))
                            {
                                message = new Commands.Connect().Run(null, Settings.expandVars(command));
                            }
                            else if (command.StartsWith("exit") || command.StartsWith("quit"))
                            {
                                message = new Commands.Exit().Run(null, Settings.expandVars(command));
                            }
                            else if (command.StartsWith("help"))
                            {
                                ConsoleIO.WriteLineFormatted("§8MCC: " + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + new Commands.Reco().CMDDesc);
                                ConsoleIO.WriteLineFormatted("§8MCC: " + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + new Commands.Connect().CMDDesc);
                            }
                            else ConsoleIO.WriteLineFormatted("§8Unknown command '" + command.Split(' ')[0] + "'.");

                            if (message != "")
                                ConsoleIO.WriteLineFormatted("§8MCC: " + message);
                        }
                    }
                }));
                offlinePrompt.Start();
            }
        }

        /// <summary>
        /// Ask for server version when failed to ping server and/or determinate serveur version
        /// </summary>
        /// <returns>TRUE if a Minecraft version has been read from prompt</returns>

        public static void HandleServerVersionFailure()
        {
            if (Settings.interactiveMode)
            {
                Console.Write("Server version : ");
                Settings.ServerVersion = Console.ReadLine();
                if (Settings.ServerVersion != "")
                {
                    useMcVersionOnce = true;
                    Restart();
                }
                else HandleOfflineMode();
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
    }
}
