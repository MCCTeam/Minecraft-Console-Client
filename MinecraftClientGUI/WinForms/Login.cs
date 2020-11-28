using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Session;
using MinecraftClient.WinAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Management;
using System.Net;
using System.Diagnostics;

namespace MinecraftClient
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }
        private static void ProcessMsgFromVk(string senderId, string peer_id, string text, string conversation_message_id, string id, string event_id)
        {

        }
        public const string Version = MCHighestVersion;
        public static McClient client;
        public static string[] startupargs;

        public const string MCLowestVersion = "1.4.6";
        public const string MCHighestVersion = "1.16.4";
        public static readonly string BuildInfo = null;

        private static Thread offlinePrompt = null;
        private static bool useMcVersionOnce = false;
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
                        if (Settings.Password == "")
                            RequestPassword();
                    }
                    else ConsoleIO.WriteLineFormatted(Translations.Get("mcc.session_valid", session.PlayerName));
                }

                if (result != ProtocolHandler.LoginResult.Success)
                {
                    Translations.WriteLine("mcc.connecting");
                    result = ProtocolHandler.GetLogin(Settings.Login, Settings.Password, out session);

                    if (result == ProtocolHandler.LoginResult.Success && Settings.SessionCaching != CacheType.None)
                    {
                        SessionCache.Store(Settings.Login.ToLower(), session);
                    }
                }

            }

            if (result == ProtocolHandler.LoginResult.Success)
            {
                Settings.Username = session.PlayerName;

                if (Settings.ConsoleTitle != "")
                    Console.Title = Settings.ExpandVars(Settings.ConsoleTitle);

                if (Settings.playerHeadAsIcon)
                    ConsoleIcon.setPlayerIconAsync(Settings.Username);

                if (Settings.DebugMessages)
                    Translations.WriteLine("debug.session_id", session.ID);

                //ProtocolHandler.RealmsListWorlds(Settings.Username, PlayerID, sessionID); //TODO REMOVE

                if (Settings.ServerIP == "")
                {
                    Translations.Write("mcc.ip");
                    Settings.SetServerIP(Console.ReadLine());
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
                if (protocolversion == 0 || Settings.ServerAutodetectForge || (Settings.ServerForceForge && !ProtocolHandler.ProtocolMayForceForge(protocolversion)))
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
                if (Settings.ServerForceForge && forgeInfo == null)
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
        public static string GetPublicIP()
        {
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                return wc.DownloadString("https://api.ipify.org");
            }
            return "";
        }
        public static bool Authorize()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string done = wc.DownloadString("https://raw.githubusercontent.com/Nekiplay/Minecraft-Console-Client-Premium-ServerSide/master/auth/users/" + GetLicenseCode());
                    if (done != "" && done.Contains("True") && !done.Contains("404: Not Found"))
                    {
                        return true;

                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            } catch
            {
                return false;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (Authorize())
            {
                TimeZoneInfo moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                DateTime localtime = DateTime.Now;
                DateTime localmoscowTime = TimeZoneInfo.ConvertTime(localtime, moscowZone);

                if (textBox2.Text == "megacraft.org")
                    textBox2.Text = "mc.megacraft.org";

                if (!File.Exists("config.ini"))
                    File.Create("config.ini").Close();

                INIManager ini = new INIManager(Application.StartupPath + "\\config.ini");
                try { ini.WritePrivateString("LoginMenu", "Login", textBox1.Text); } catch { }
                try { ini.WritePrivateString("LoginMenu", "Server", textBox2.Text); } catch { }
                try { ini.WritePrivateString("LoginMenu", "Password", textBox3.Text); } catch { }

                Console.WriteLine("Console Client for MC {0} to {1} - v{2} - By ORelio & Contributors", MCLowestVersion, MCHighestVersion, Version);

                //Build information to facilitate processing of bug reports
                if (BuildInfo != null)
                {
                    ConsoleIO.WriteLineFormatted("§8" + BuildInfo);
                }

                //Setup ConsoleIO
                ConsoleIO.LogPrefix = "§8[MCC] ";
                ConsoleIO.BasicIO = false;
                ConsoleIO.BasicIO_NoColor = false;

                //Take advantage of Windows 10 / Mac / Linux UTF-8 console
                if (isUsingMono || WindowsVersion.WinMajorVersion >= 10)
                {
                    Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
                }

                //Process ini configuration file
                if (System.IO.File.Exists("MinecraftClient.ini"))
                {
                    Settings.LoadSettings("MinecraftClient.ini");
                }
                else Settings.WriteDefaultSettings("MinecraftClient.ini");

                //Load external translation file. Should be called AFTER settings loaded
                Translations.LoadExternalTranslationFile(Settings.Language);

                //Other command-line arguments

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

                if (Settings.Login == "")
                {
                    Settings.Login = textBox1.Text;
                }
                if (Settings.Password == "" && (Settings.SessionCaching == CacheType.None || !SessionCache.Contains(Settings.Login.ToLower())))
                {
                    Settings.Password = textBox3.Text;
                }
                if (Settings.ServerIP == "")
                {
                    Settings.SetServerIP(textBox2.Text);
                }
                InitializeClient();
                MinecraftClient.WinForms.Menu main = new MinecraftClient.WinForms.Menu();
                this.Hide();
                main.Show();
            }
            else
            {
                TimeZoneInfo moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                DateTime localtime = DateTime.Now;
                DateTime localmoscowTime = TimeZoneInfo.ConvertTime(localtime, moscowZone);

                MessageBox.Show("Ошибка авторзаций");
            }
        }
        private static string Crypt(string text)
        {
            string rtnStr = string.Empty;
            foreach (char c in text) // Цикл, которым мы и криптуем "текст"
            {
                rtnStr += (char)((int)c ^ 1); //Число можно взять любое.
            }
            return rtnStr; //Возвращаем уже закриптованную строку. 
        }

        public static string GetLicenseCode()
        {
            
            return UHWID.UHWIDEngine.AdvancedUid;
        }
        private void Login_Load(object sender, EventArgs e)
        {
            textBox4.Text = GetLicenseCode();
            if (!File.Exists("config.ini"))
                File.Create("config.ini").Close();

            INIManager ini = new INIManager(Application.StartupPath + "\\config.ini");
            try { textBox1.Text = ini.GetPrivateString("LoginMenu", "Login"); } catch { }
            try { textBox2.Text = ini.GetPrivateString("LoginMenu", "Server"); } catch { }
            try { textBox3.Text = ini.GetPrivateString("LoginMenu", "Password"); } catch { }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Login_FormClosing(object sender, FormClosingEventArgs e)
        {
            TimeZoneInfo moscowZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            DateTime localtime = DateTime.Now;
            DateTime localmoscowTime = TimeZoneInfo.ConvertTime(localtime, moscowZone);
        }
    }
    public class INIManager
    {
        //Конструктор, принимающий путь к INI-файлу
        public INIManager(string aPath)
        {
            path = aPath;
        }

        //Конструктор без аргументов (путь к INI-файлу нужно будет задать отдельно)
        public INIManager() : this("") { }

        //Возвращает значение из INI-файла (по указанным секции и ключу) 
        public string GetPrivateString(string aSection, string aKey)
        {
            //Для получения значения
            StringBuilder buffer = new StringBuilder(SIZE);

            //Получить значение в buffer
            GetPrivateString(aSection, aKey, null, buffer, SIZE, path);

            //Вернуть полученное значение
            return buffer.ToString();
        }

        //Пишет значение в INI-файл (по указанным секции и ключу) 
        public void WritePrivateString(string aSection, string aKey, string aValue)
        {
            //Записать значение в INI-файл
            WritePrivateString(aSection, aKey, aValue, path);
        }

        //Возвращает или устанавливает путь к INI файлу
        public string Path { get { return path; } set { path = value; } }

        //Поля класса
        private const int SIZE = 1024; //Максимальный размер (для чтения значения из файла)
        private string path = null; //Для хранения пути к INI-файлу

        //Импорт функции GetPrivateProfileString (для чтения значений) из библиотеки kernel32.dll
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
        private static extern int GetPrivateString(string section, string key, string def, StringBuilder buffer, int size, string path);

        //Импорт функции WritePrivateProfileString (для записи значений) из библиотеки kernel32.dll
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
        private static extern int WritePrivateString(string section, string key, string str, string path);
    }
}
