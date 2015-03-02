﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MinecraftClient
{
    ///
    /// Welcome to the Bot API file !
    /// The virtual class "ChatBot" contains anything you need for creating chat bots
    /// Inherit from this class while adding your bot class to the folder "ChatBots".
    /// Override the methods you want for handling events: Initialize, Update, GetText.
    /// Once your bot is created, read the explanations below to start using it in the MinecraftClient app.
    ///
    /// Pieces of code to add in other parts of the program for your bot. Line numbers are approximative.
    /// McTcpClient:110 | if (Settings.YourBot_Enabled) { handler.BotLoad(new ChatBots.YourBot()); }
    /// Settings.cs:73  | public static bool YourBot_Enabled = false;
    /// Settings.cs:74  | private enum ParseMode { /* [...] */, YourBot };
    /// Settings.cs:106 | case "yourbot": pMode = ParseMode.YourBot; break;
    /// Settings.cs:197 | case ParseMode.YourBot: switch (argName.ToLower()) { case "enabled": YourBot_Enabled = str2bool(argValue); break; } break;
    /// Settings.cs:267 | + "[YourBot]\r\n" + "enabled=false\r\n"
    /// Here your are. Now you will have a setting in MinecraftClient.ini for enabling your brand new bot.
    /// Delete MinecraftClient.ini to re-generate it or add the lines [YourBot] and enabled=true to the existing one.
    ///

    /// <summary>
    /// The virtual class containing anything you need for creating chat bots.
    /// </summary>

    public abstract class ChatBot
    {
        public enum DisconnectReason { InGameKick, LoginRejected, ConnectionLost };

        //Will be automatically set on bot loading, don't worry about this
        public void SetHandler(McTcpClient handler) { this.handler = handler; }
        private McTcpClient handler;

        /* ================================================== */
        /*   Main methods to override for creating your bot   */
        /* ================================================== */

        /// <summary>
        /// Anything you want to initialize your bot, will be called on load by MinecraftCom
        /// </summary>

        public virtual void Initialize() { }

        /// <summary>
        /// Will be called every ~100ms (10fps) if loaded in MinecraftCom
        /// </summary>

        public virtual void Update() { }

        /// <summary>
        /// Any text sent by the server will be sent here by MinecraftCom
        /// </summary>
        /// <param name="text">Text from the server</param>

        public virtual void GetText(string text) { }

        /// <summary>
        /// Is called when the client has been disconnected fom the server
        /// </summary>
        /// <param name="reason">Disconnect Reason</param>
        /// <param name="message">Kick message, if any</param>
        /// <returns>Return TRUE if the client is about to restart</returns>

        public virtual bool OnDisconnect(DisconnectReason reason, string message) { return false; }

        /* =================================================================== */
        /*  ToolBox - Methods below might be useful while creating your bot.   */
        /*  You should not need to interact with other classes of the program. */
        /*  All the methods in this ChatBot class should do the job for you.   */
        /* =================================================================== */

        /// <summary>
        /// Send text to the server. Can be anything such as chat messages or commands
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        /// <returns>True if the text was sent with no error</returns>

        protected bool SendText(string text)
        {
            LogToConsole("Sending '" + text + "'");
            return handler.SendText(text);
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>

        protected bool performInternalCommand(string command)
        {
            string temp = "";
            return handler.performInternalCommand(command, ref temp);
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command to process</param>
        /// <param name="response_msg">May contain a confirmation or error message after processing the command, or "" otherwise.</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>

        protected bool performInternalCommand(string command, ref string response_msg)
        {
            return handler.performInternalCommand(command, ref response_msg);
        }

        /// <summary>
        /// Remove color codes ("§c") from a text message received from the server
        /// </summary>

        protected static string getVerbatim(string text)
        {
            if ( String.IsNullOrEmpty(text) )
                return String.Empty;

            int idx = 0;
            var data = new char[text.Length];

            for ( int i = 0; i < text.Length; i++ )
                if ( text[i] != '§' )
                    data[idx++] = text[i];
                else
                    i++;

            return new string(data, 0, idx);
        }

        /// <summary>
        /// Verify that a string contains only a-z A-Z 0-9 and _ characters.
        /// </summary>

        protected static bool isValidName(string username)
        {
            if ( String.IsNullOrEmpty(username) )
                return false;

            foreach ( char c in username )
                if ( !((c >= 'a' && c <= 'z')
                        || (c >= 'A' && c <= 'Z')
                        || (c >= '0' && c <= '9')
                        || c == '_') )
                    return false;

            return true;
        }

        /// <summary>
        /// Returns true if the text passed is a private message sent to the bot
        /// </summary>
        /// <param name="text">text to test</param>
        /// <param name="message">if it's a private message, this will contain the message</param>
        /// <param name="sender">if it's a private message, this will contain the player name that sends the message</param>
        /// <returns>Returns true if the text is a private message</returns>

        protected static bool isPrivateMessage(string text, ref string message, ref string sender)
        {
            text = getVerbatim(text);
            if (text == "") { return false; }
            string[] tmp = text.Split(' ');

            try
            {
                //Detect vanilla /tell messages
                //Someone whispers message (MC 1.5)
                //Someone whispers to you: message (MC 1.7)
                if (tmp.Length > 2 && tmp[1] == "whispers")
                {
                    if (tmp.Length > 4 && tmp[2] == "to" && tmp[3] == "you:")
                    {
                        message = text.Substring(tmp[0].Length + 18); //MC 1.7
                    }
                    else message = text.Substring(tmp[0].Length + 10); //MC 1.5
                    sender = tmp[0];
                    return isValidName(sender);
                }

                //Detect Essentials (Bukkit) /m messages
                //[Someone -> me] message
                //[~Someone -> me] message
                else if (text[0] == '[' && tmp.Length > 3 && tmp[1] == "->"
                        && (tmp[2] == "me]" || tmp[2] == "moi]")) //'me' is replaced by 'moi' in french servers
                {
                    message = text.Substring(tmp[0].Length + 4 + tmp[2].Length + 1);
                    sender = tmp[0].Substring(1);
                    if (sender[0] == '~') { sender = sender.Substring(1); }
                    return isValidName(sender);
                }

                //Detect Essentials (Bukkit) /me messages with some custom rank
                //[Someone [rank] -> me] message
                //[~Someone [rank] -> me] message
                else if (text[0] == '[' && tmp.Length > 3 && tmp[2] == "->"
                        && (tmp[3] == "me]" || tmp[3] == "moi]")) //'me' is replaced by 'moi' in french servers
                {
                    message = text.Substring(tmp[0].Length + 1 + tmp[1].Length + 4 + tmp[2].Length + 1);
                    sender = tmp[0].Substring(1);
                    if (sender[0] == '~') { sender = sender.Substring(1); }
                    return isValidName(sender);
                }

                //Detect HeroChat PMsend
                //From Someone: message
                else if (text.StartsWith("From "))
                {
                    sender = text.Substring(5).Split(':')[0];
                    message = text.Substring(text.IndexOf(':') + 2);
                    return isValidName(sender);
                }

                //Detect HeroChat Messages
                //[Channel] [Rank] User: Message
                else if (text.StartsWith("[") && text.Contains(':') && tmp.Length > 2)
                {
                    int name_end = text.IndexOf(':');
                    int name_start = text.Substring(0, name_end).LastIndexOf(']') + 2;
                    sender = text.Substring(name_start, name_end - name_start);
                    message = text.Substring(name_end + 2);
                    return isValidName(sender);
                }
                //Detect Essentials Pay message
                //${amount} has been received from {user}.
                else if (text.StartsWith("$"))
                {
                    sender = tmp[4].ToString().Remove(tmp[4].ToString().Length - 1);
                    message = tmp[0].ToString();
                    return isValidName(sender);
                }


                else return false;
            }
            catch (IndexOutOfRangeException) { return false; }
        }

        /// <summary>
        /// Returns true if the text passed is a public message written by a player on the chat
        /// </summary>
        /// <param name="text">text to test</param>
        /// <param name="message">if it's message, this will contain the message</param>
        /// <param name="sender">if it's message, this will contain the player name that sends the message</param>
        /// <returns>Returns true if the text is a chat message</returns>

        protected static bool isChatMessage(string text, ref string message, ref string sender)
        {
            //Detect chat messages
            //<Someone> message
            //<*Faction Someone> message
            //<*Faction Someone>: message
            //<*Faction ~Nicknamed>: message
            text = getVerbatim(text);
            if (text == "") { return false; }
            if (text[0] == '<')
            {
                try
                {
                    text = text.Substring(1);
                    string[] tmp = text.Split('>');
                    sender = tmp[0];
                    message = text.Substring(sender.Length + 2);
                    if (message.Length > 1 && message[0] == ' ')
                    { message = message.Substring(1); }
                    tmp = sender.Split(' ');
                    sender = tmp[tmp.Length - 1];
                    if (sender[0] == '~') { sender = sender.Substring(1); }
                    return isValidName(sender);
                }
                catch (IndexOutOfRangeException) { return false; }
            }
            else return false;
        }

        /// <summary>
        /// Returns true if the text passed is a teleport request (Essentials)
        /// </summary>
        /// <param name="text">Text to parse</param>
        /// <param name="sender">Will contain the sender's username, if it's a teleport request</param>
        /// <returns>Returns true if the text is a teleport request</returns>

        protected static bool isTeleportRequest(string text, ref string sender)
        {
            text = getVerbatim(text);
            sender = text.Split(' ')[0];
            if (text.EndsWith("has requested to teleport to you.")
             || text.EndsWith("has requested that you teleport to them."))
            {
                return isValidName(sender);
            }
            else return false;
        }

        /// <summary>
        /// Writes some text in the console. Nothing will be sent to the server.
        /// </summary>
        /// <param name="text">Log text to write</param>

        public static void LogToConsole(string text)
        {
            ConsoleIO.WriteLineFormatted("§8[BOT] " + text);
            string logfile = Settings.expandVars(Settings.chatbotLogFile);

            if (!String.IsNullOrEmpty(logfile))
            {
                if (!File.Exists(logfile))
                {
                    try { Directory.CreateDirectory(Path.GetDirectoryName(logfile)); }
                    catch { return; /* Invalid path or access denied */ }
                    try { File.WriteAllText(logfile, ""); }
                    catch { return; /* Invalid file name or access denied */ }
                }

                File.AppendAllLines(logfile, new string[] { getTimestamp() + ' ' + text });
            }
        }

        /// <summary>
        /// Disconnect from the server and restart the program
        /// It will unload & reload all the bots and then reconnect to the server
        /// </summary>

        protected void ReconnectToTheServer() { ReconnectToTheServer(3); }

        /// <summary>
        /// Disconnect from the server and restart the program
        /// It will unload & reload all the bots and then reconnect to the server
        /// </summary>
        /// <param name="attempts">If connection fails, the client will make X extra attempts</param>

        protected void ReconnectToTheServer(int ExtraAttempts)
        {
            McTcpClient.AttemptsLeft = ExtraAttempts;
            Program.Restart();
        }

        /// <summary>
        /// Disconnect from the server and exit the program
        /// </summary>

        protected void DisconnectAndExit()
        {
            Program.Exit();
        }

        /// <summary>
        /// Unload the chatbot, and release associated memory.
        /// </summary>

        protected void UnloadBot()
        {
            handler.BotUnLoad(this);
        }

        /// <summary>
        /// Send a private message to a player
        /// </summary>
        /// <param name="player">Player name</param>
        /// <param name="message">Message</param>

        protected void SendPrivateMessage(string player, string message)
        {
            SendText("/tell " + player + ' ' + message);
        }

        /// <summary>
        /// Run a script from a file using a Scripting bot
        /// </summary>
        /// <param name="filename">File name</param>
        /// <param name="playername">Player name to send error messages, if applicable</param>

        protected void RunScript(string filename, string playername = "")
        {
            handler.BotLoad(new ChatBots.Script(filename, playername));
        }

        /// <summary>
        /// Get a D-M-Y h:m:s timestamp representing the current system date and time
        /// </summary>

        protected static string getTimestamp()
        {
            DateTime time = DateTime.Now;

            string D = time.Day.ToString("00");
            string M = time.Month.ToString("00");
            string Y = time.Year.ToString("0000");

            string h = time.Hour.ToString("00");
            string m = time.Minute.ToString("00");
            string s = time.Second.ToString("00");

            return "" + D + '-' + M + '-' + Y + ' ' + h + ':' + m + ':' + s;
        }
    }
}
