using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using MinecraftClient.Protocol;
using MinecraftClient.Proxy;

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// </summary>

    public class McTcpClient : IMinecraftComHandler
    {
        private List<ChatBot> bots = new List<ChatBot>();
        private static List<ChatBots.Script> scripts_on_hold = new List<ChatBots.Script>();
        public void BotLoad(ChatBot b) { b.SetHandler(this); bots.Add(b); b.Initialize(); Settings.SingleCommand = ""; }
        public void BotUnLoad(ChatBot b) { bots.RemoveAll(item => object.ReferenceEquals(item, b)); }
        public void BotClear() { bots.Clear(); }

        public static int AttemptsLeft = 0;

        private string host;
        private int port;
        private string username;
        private string uuid;
        private string sessionid;

        public int getServerPort() { return port; }
        public string getServerHost() { return host; }
        public string getUsername() { return username; }
        public string getUserUUID() { return uuid; }
        public string getSessionID() { return sessionid; }
        
        TcpClient client;
        IMinecraftCom handler;

        /// <summary>
        /// Starts the main chat client
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="server_port">The server IP (serveradress or serveradress:port)</param>

        public McTcpClient(string username, string uuid, string sessionID, int protocolversion, string server_port)
        {
            StartClient(username, uuid, sessionID, server_port, protocolversion, false, "");
        }

        /// <summary>
        /// Starts the main chat client in single command sending mode
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="server_port">The server IP (serveradress or serveradress:port)</param>
        /// <param name="command">The text or command to send.</param>

        public McTcpClient(string username, string uuid, string sessionID, string server_port, int protocolversion, string command)
        {
            StartClient(username, uuid, sessionID, server_port, protocolversion, true, command);
        }

        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="user">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="server_port">The server IP (serveradress or serveradress:port)/param>
        /// <param name="singlecommand">If set to true, the client will send a single command and then disconnect from the server</param>
        /// <param name="command">The text or command to send. Will only be sent if singlecommand is set to true.</param>

        private void StartClient(string user, string uuid, string sessionID, string server_port, int protocolversion, bool singlecommand, string command)
        {
            string[] sip = server_port.Split(':');

            this.sessionid = sessionID;
            this.uuid = uuid;
            this.username = user;
            this.host = sip[0];

            if (sip.Length == 1)
            {
                port = 25565;
            }
            else
            {
                try
                {
                    port = Convert.ToInt32(sip[1]);
                }
                catch (FormatException) { port = 25565; }
            }

            if (!singlecommand)
            {
                if (Settings.AntiAFK_Enabled) { BotLoad(new ChatBots.AntiAFK(Settings.AntiAFK_Delay)); }
                if (Settings.Hangman_Enabled) { BotLoad(new ChatBots.HangmanGame(Settings.Hangman_English)); }
                if (Settings.Alerts_Enabled) { BotLoad(new ChatBots.Alerts()); }
                if (Settings.ChatLog_Enabled) { BotLoad(new ChatBots.ChatLog(Settings.ChatLog_File.Replace("%username%", Settings.Username), Settings.ChatLog_Filter, Settings.ChatLog_DateTime)); }
                if (Settings.PlayerLog_Enabled) { BotLoad(new ChatBots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.PlayerLog_File.Replace("%username%", Settings.Username))); }
                if (Settings.AutoRelog_Enabled) { BotLoad(new ChatBots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries)); }
                if (Settings.ScriptScheduler_Enabled) { BotLoad(new ChatBots.ScriptScheduler(Settings.ScriptScheduler_TasksFile.Replace("%username%", Settings.Username))); }
                if (Settings.RemoteCtrl_Enabled) { BotLoad(new ChatBots.RemoteControl()); }
            }

            try
            {
                client = ProxyHandler.newTcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;
                handler = Protocol.ProtocolHandler.getProtocolHandler(client, protocolversion, this);
                Console.WriteLine("Version is supported.\nLogging in...");
                
                if (handler.Login())
                {
                    if (singlecommand)
                    {
                        handler.SendChatMessage(command);
                        ConsoleIO.WriteLineFormatted("§7Command §8" + command + "§7 sent.", false);
                        Thread.Sleep(5000);
                        handler.Disconnect();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        foreach (ChatBot bot in scripts_on_hold) { bots.Add(bot); }
                        scripts_on_hold.Clear();
                        Console.WriteLine("Server was successfully joined.\nType '/quit' to leave the server.");
                        StartTalk();
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Failed to connect to this IP.");
                if (AttemptsLeft > 0)
                {
                    ChatBot.LogToConsole("Waiting 5 seconds (" + AttemptsLeft + " attempts left)...");
                    Thread.Sleep(5000); AttemptsLeft--; Program.Restart();
                }
                else if (!singlecommand) { Console.ReadLine(); }
            }
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and to leave the server.
        /// </summary>

        private void StartTalk()
        {
            try
            {
                string text = "";
                Thread.Sleep(500);
                handler.SendRespawnPacket();

                while (client.Client.Connected)
                {
                    text = ConsoleIO.ReadLine();
                    if (ConsoleIO.basicIO && text.Length > 0 && text[0] == (char)0x00)
                    {
                        //Process a request from the GUI
                        string[] command = text.Substring(1).Split((char)0x00);
                        switch (command[0].ToLower())
                        {
                            case "autocomplete":
                                if (command.Length > 1) { ConsoleIO.WriteLine((char)0x00 + "autocomplete" + (char)0x00 + handler.AutoComplete(command[1])); }
                                else Console.WriteLine((char)0x00 + "autocomplete" + (char)0x00);
                                break;
                        }
                    }
                    else
                    {
                        text = text.Trim();
                        if (text.ToLower() == "/quit" || text.ToLower() == "/reco")
                        {
                            break;
                        }
                        else if (text.ToLower() == "/respawn")
                        {
                            handler.SendRespawnPacket();
                            ConsoleIO.WriteLine("You have respawned.");
                        }
                        else if (text.ToLower().StartsWith("/script "))
                        {
                            BotLoad(new ChatBots.Script(text.Substring(8)));
                        }
                        else if (text != "")
                        {
                            //Message is too long
                            if (text.Length > 100)
                            {
                                if (text[0] == '/')
                                {
                                    //Send the first 100 chars of the command
                                    text = text.Substring(0, 100);
                                    handler.SendChatMessage(text);
                                }
                                else
                                {
                                    //Send the message splitted into several messages
                                    while (text.Length > 100)
                                    {
                                        handler.SendChatMessage(text.Substring(0, 100));
                                        text = text.Substring(100, text.Length - 100);
                                    }
                                    handler.SendChatMessage(text);
                                }
                            }
                            else handler.SendChatMessage(text);
                        }
                    }
                }

                switch (text.ToLower())
                {
                    case "/quit": Program.Exit(); break;
                    case "/reco": Program.Restart(); break;
                }
            }
            catch (IOException) { }
        }

        /// <summary>
        /// Disconnect the client from the server
        /// </summary>

        public void Disconnect()
        {
            foreach (ChatBot bot in bots)
                if (bot is ChatBots.Script)
                    scripts_on_hold.Add((ChatBots.Script)bot);

            handler.Disconnect();
            handler.Dispose();
            Thread.Sleep(1000);

            if (client != null) { client.Close(); }
        }

        /// <summary>
        /// Received some text from the server
        /// </summary>
        /// <param name="text">Text received</param>

        public void OnTextReceived(string text)
        {
            ConsoleIO.WriteLineFormatted(text, false);
            foreach (ChatBot bot in bots)
                bot.GetText(text);
        }

        /// <summary>
        /// When connection has been lost
        /// </summary>

        public void OnConnectionLost(ChatBot.DisconnectReason reason, string message)
        {
            bool will_restart = false;

            switch (reason)
            {
                case ChatBot.DisconnectReason.ConnectionLost:
                    message = "Connection has been lost.";
                    ConsoleIO.WriteLine(message);
                    break;

                case ChatBot.DisconnectReason.InGameKick:
                    ConsoleIO.WriteLine("Disconnected by Server :");
                    ConsoleIO.WriteLineFormatted(message, true);
                    break;

                case ChatBot.DisconnectReason.LoginRejected:
                    ConsoleIO.WriteLine("Login failed :");
                    ConsoleIO.WriteLineFormatted(message, true);
                    break;
            }

            foreach (ChatBot bot in bots)
                will_restart |= bot.OnDisconnect(reason, message);

            if (!will_restart) { Program.ReadLineReconnect(); }
        }

        /// <summary>
        /// Called ~10 times per second by the protocol handler
        /// </summary>

        public void OnUpdate()
        {
            for (int i = 0; i < bots.Count; i++)
                bots[i].Update();
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>

        public void SendChatMessage(string message)
        {
            handler.SendChatMessage(message);
        }
    }
}
