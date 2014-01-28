using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient
{
    ///
    /// Welcome to the Bot API file !
    /// The virtual class "ChatBot" contains anything you need for creating chat bots
    /// Inherit from this class while adding your bot class to the namespace "Bots", below.
    /// Once your bot is created, simply edit the switch in Program.cs to add the corresponding command-line argument!
    ///

    /// <summary>
    /// The virtual class containing anything you need for creating chat bots.
    /// </summary>

    public abstract class ChatBot
    {
        public enum DisconnectReason { InGameKick, LoginRejected, ConnectionLost };

        #region MinecraftCom Handler for this bot

        //Will be automatically set on bot loading, don't worry about this
        public void SetHandler(MinecraftCom handler) { this.handler = handler; }
        private MinecraftCom handler;

        #endregion

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

        #region ToolBox

        /// <summary>
        /// Send text to the server. Can be anything such as chat messages or commands
        /// </summary>
        /// <param name="text">Text to send to the server</param>

        protected void SendText(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            ConsoleIO.WriteLine("BOT:" + text);
            handler.SendChatMessage(text);
            Console.ForegroundColor = ConsoleColor.Gray;
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
        /// Returns true is the text passed is a private message sent to the bot
        /// </summary>
        /// <param name="text">text to test</param>
        /// <param name="message">if it's a private message, this will contain the message</param>
        /// <param name="sender">if it's a private message, this will contain the player name that sends the message</param>
        /// <returns>Returns true if the text is a private message</returns>

        protected static bool isPrivateMessage(string text, ref string message, ref string sender)
        {
            if (text == "") { return false; }
            string[] tmp = text.Split(' ');

            try
            {
                //Detect vanilla /tell messages
                //Someone whispers to you: message
                if (tmp.Length > 2 && tmp[1] == "whispers")
                {
                    message = text.Substring(tmp[0].Length + 18);
                    sender = tmp[0];
                    return isValidName(sender);
                }

                //Detect Essentials (Bukkit) /m messages
                //[Someone -> me] message
                else if (text[0] == '[' && tmp.Length > 3 && tmp[1] == "->"
                        && (tmp[2] == "me]" || tmp[2] == "moi]")) //'me' is replaced by 'moi' in french servers
                {
                    message = text.Substring(tmp[0].Length + 4 + tmp[2].Length + 1);
                    sender = tmp[0].Substring(1);
                    if (sender[0] == '~') { sender = sender.Substring(1); }
                    return isValidName(sender);
                }
                else return false;
            }
            catch (IndexOutOfRangeException) { return false; }
        }

        /// <summary>
        /// Returns true is the text passed is a public message written by a player on the chat
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
        /// Writes some text in the console. Nothing will be sent to the server.
        /// </summary>
        /// <param name="text">Log text to write</param>

        public static void LogToConsole(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            ConsoleIO.WriteLine("[BOT] " + text);
            Console.ForegroundColor = ConsoleColor.Gray;
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

        #endregion
    }

    namespace Bots
    {
        /// <summary>
        /// Example of message receiving.
        /// </summary>

        public class TestBot : ChatBot
        {
            public override void GetText(string text)
            {
                string message = "";
                string username = "";
                text = getVerbatim(text);

                if (isPrivateMessage(text, ref message, ref username))
                {
                    ConsoleIO.WriteLine("Bot: " + username + " told me : " + message);
                }
                else if (isChatMessage(text, ref message, ref username))
                {
                    ConsoleIO.WriteLine("Bot: " + username + " said : " + message);
                }
            }
        }

        /// <summary>
        /// This bot sends a command every 60 seconds in order to stay non-afk.
        /// </summary>

        public class AntiAFK : ChatBot
        {
            private int count;
            private int timeping;

            /// <summary>
            /// This bot sends a /ping command every X seconds in order to stay non-afk.
            /// </summary>
            /// <param name="pingparam">Time amount between each ping (10 = 1s, 600 = 1 minute, etc.)</param>

            public AntiAFK(int pingparam)
            {
                count = 0;
                timeping = pingparam;
                if (timeping < 10) { timeping = 10; } //To avoid flooding
            }

            public override void Update()
            {
                count++;
                if (count == timeping)
                {
                    SendText(Settings.AntiAFK_Command);
                    count = 0;
                }
            }
        }

        /// <summary>
        /// This bot sends a /list command every X seconds and save the result.
        /// </summary>

        public class PlayerListLogger : ChatBot
        {
            private int count;
            private int timeping;
            private string file;

            /// <summary>
            /// This bot sends a  /list command every X seconds and save the result.
            /// </summary>
            /// <param name="pingparam">Time amount between each list ping (10 = 1s, 600 = 1 minute, etc.)</param>

            public PlayerListLogger(int pingparam, string filetosavein)
            {
                count = 0;
                file = filetosavein;
                timeping = pingparam;
                if (timeping < 10) { timeping = 10; } //To avoid flooding

            }

            public override void Update()
            {
                count++;
                if (count == timeping)
                {
                    SendText("/list");
                    count = 0;
                }
            }

            public override void GetText(string text)
            {
                if (text.Contains("Joueurs en ligne") || text.Contains("Connected:") || text.Contains("online:"))
                {
                    LogToConsole("Saving Player List");
                    DateTime now = DateTime.Now;
                    string TimeStamp = "[" + now.Year + '/' + now.Month + '/' + now.Day + ' ' + now.Hour + ':' + now.Minute + ']';
                    System.IO.File.AppendAllText(file, TimeStamp + "\n" + getVerbatim(text) + "\n\n");
                }
            }
        }

        /// <summary>
        /// "Le jeu du Pendu" (Hangman game)
        /// </summary>

        public class Pendu : ChatBot
        {
            private int vie = 0;
            private int vie_param = 10;
            private int compteur = 0;
            private int compteur_param = 3000; //5 minutes
            private bool running = false;
            private bool[] discovered;
            private string word = "";
            private string letters = "";
            private string[] owners;
            private bool English;

            /// <summary>
            /// "Le jeu du Pendu" (Hangman Game)
            /// </summary>
            /// <param name="english">if true, the game will be in english. If false, the game will be in french.</param>

            public Pendu(bool english)
            {
                English = english;
            }

            public override void Initialize()
            {
                owners = getowners();
            }

            public override void Update()
            {
                if (running)
                {
                    if (compteur > 0)
                    {
                        compteur--;
                    }
                    else
                    {
                        SendText(English ? "You took too long to try a letter." : "Temps imparti écoulé !");
                        SendText(English ? "Game canceled." : "Partie annulée.");
                        running = false;
                    }
                }
            }

            public override void GetText(string text)
            {
                string message = "";
                string username = "";
                text = getVerbatim(text);

                if (isPrivateMessage(text, ref message, ref username))
                {
                    if (owners.Contains(username.ToUpper()))
                    {
                        switch (message)
                        {
                            case "start":
                                start();
                                break;
                            case "stop":
                                running = false;
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    if (running && isChatMessage(text, ref message, ref username))
                    {
                        if (message.Length == 1)
                        {
                            char letter = message.ToUpper()[0];
                            if (letter >= 'A' && letter <= 'Z')
                            {
                                if (letters.Contains(letter))
                                {
                                    SendText(English ? ("Letter " + letter + " has already been tried.") : ("Le " + letter + " a déjà été proposé."));
                                }
                                else
                                {
                                    letters += letter;
                                    compteur = compteur_param;

                                    if (word.Contains(letter))
                                    {
                                        for (int i = 0; i < word.Length; i++) { if (word[i] == letter) { discovered[i] = true; } }
                                        SendText(English ? ("Yes, the word contains a " + letter + '!') : ("Le " + letter + " figurait bien dans le mot :)"));
                                    }
                                    else
                                    {
                                        vie--;
                                        if (vie == 0)
                                        {
                                            SendText(English ? "Game Over! :]" : "Perdu ! Partie terminée :]");
                                            SendText(English ? ("The word was: " + word) : ("Le mot était : " + word));
                                            running = false;
                                        }
                                        else SendText(English ? ("The " + letter + "? No.") : ("Le " + letter + " ? Non."));
                                    }

                                    if (running)
                                    {
                                        SendText(English ? ("Mysterious word: " + word_cached + " (lives : " + vie + ")")
                                        : ("Mot mystère : " + word_cached + " (vie : " + vie + ")"));
                                    }

                                    if (winner)
                                    {
                                        SendText(English ? ("Congrats, " + username + '!') : ("Félicitations, " + username + " !"));
                                        running = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private void start()
            {
                vie = vie_param;
                running = true;
                letters = "";
                word = chooseword();
                compteur = compteur_param;
                discovered = new bool[word.Length];

                SendText(English ? "Hangman v1.0 - By ORelio" : "Pendu v1.0 - Par ORelio");
                SendText(English ? ("Mysterious word: " + word_cached + " (lives : " + vie + ")")
                : ("Mot mystère : " + word_cached + " (vie : " + vie + ")"));
                SendText(English ? ("Try some letters ... :)") : ("Proposez une lettre ... :)"));
            }

            private string chooseword()
            {
                if (System.IO.File.Exists(English ? Settings.Hangman_FileWords_EN : Settings.Hangman_FileWords_FR))
                {
                    string[] dico = System.IO.File.ReadAllLines(English ? Settings.Hangman_FileWords_EN : Settings.Hangman_FileWords_FR);
                    return dico[new Random().Next(dico.Length)];
                }
                else
                {
                    LogToConsole(English ? "File not found: " + Settings.Hangman_FileWords_EN : "Fichier introuvable : " + Settings.Hangman_FileWords_FR);
                    return English ? "WORDSAREMISSING" : "DICOMANQUANT";
                }
            }

            private string[] getowners()
            {
                List<string> owners = new List<string>();
                owners.Add("CONSOLE");
                if (System.IO.File.Exists(Settings.Bots_OwnersFile))
                {
                    foreach (string s in System.IO.File.ReadAllLines(Settings.Bots_OwnersFile))
                    {
                        owners.Add(s.ToUpper());
                    }
                }
                else LogToConsole(English ? "File not found: " + Settings.Bots_OwnersFile : "Fichier introuvable : " + Settings.Bots_OwnersFile);
                return owners.ToArray();
            }

            private string word_cached
            {
                get
                {
                    string printed = "";
                    for (int i = 0; i < word.Length; i++)
                    {
                        if (discovered[i])
                        {
                            printed += word[i];
                        }
                        else printed += '_';
                    }
                    return printed;
                }
            }

            private bool winner
            {
                get
                {
                    for (int i = 0; i < discovered.Length; i++)
                    {
                        if (!discovered[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// This bot make the console beep on some specified words. Useful to detect when someone is talking to you, for example.
        /// </summary>

        public class Alerts : ChatBot
        {
            private string[] dictionary = new string[0];
            private string[] excludelist = new string[0];

            public override void Initialize()
            {
                if (System.IO.File.Exists(Settings.Alerts_MatchesFile))
                {
                    dictionary = System.IO.File.ReadAllLines(Settings.Alerts_MatchesFile);

                    for (int i = 0; i < dictionary.Length; i++)
                    {
                        dictionary[i] = dictionary[i].ToLower();
                    }
                }
                else LogToConsole("File not found: " + Settings.Alerts_MatchesFile);

                if (System.IO.File.Exists(Settings.Alerts_ExcludesFile))
                {
                    excludelist = System.IO.File.ReadAllLines(Settings.Alerts_ExcludesFile);

                    for (int i = 0; i < excludelist.Length; i++)
                    {
                        excludelist[i] = excludelist[i].ToLower();
                    }
                }
                else LogToConsole("File not found : " + Settings.Alerts_ExcludesFile);
            }

            public override void GetText(string text)
            {
                text = getVerbatim(text);
                string comp = text.ToLower();
                foreach (string alert in dictionary)
                {
                    if (comp.Contains(alert))
                    {
                        bool ok = true;

                        foreach (string exclusion in excludelist)
                        {
                            if (comp.Contains(exclusion))
                            {
                                ok = false;
                                break;
                            }
                        }

                        if (ok)
                        {
                            Console.Beep(); //Text found !

                            if (ConsoleIO.basicIO) { ConsoleIO.WriteLine(comp.Replace(alert, "§c" + alert + "§r")); } else {

                            #region Displaying the text with the interesting part highlighted

                            Console.BackgroundColor = ConsoleColor.DarkGray;
                            Console.ForegroundColor = ConsoleColor.White;

                            //Will be used for text displaying
                            string[] temp = comp.Split(alert.Split(','), StringSplitOptions.RemoveEmptyEntries);
                            int p = 0;

                            //Special case : alert in the beginning of the text
                            string test = "";
                            for (int i = 0; i < alert.Length; i++)
                            {
                                test += comp[i];
                            }
                            if (test == alert)
                            {
                                Console.BackgroundColor = ConsoleColor.Yellow;
                                Console.ForegroundColor = ConsoleColor.Red;
                                for (int i = 0; i < alert.Length; i++)
                                {
                                    ConsoleIO.Write(text[p]);
                                    p++;
                                }
                            }

                            //Displaying the rest of the text
                            for (int i = 0; i < temp.Length; i++)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkGray;
                                Console.ForegroundColor = ConsoleColor.White;
                                for (int j = 0; j < temp[i].Length; j++)
                                {
                                    ConsoleIO.Write(text[p]);
                                    p++;
                                }
                                Console.BackgroundColor = ConsoleColor.Yellow;
                                Console.ForegroundColor = ConsoleColor.Red;
                                try
                                {
                                    for (int j = 0; j < alert.Length; j++)
                                    {
                                        ConsoleIO.Write(text[p]);
                                        p++;
                                    }
                                }
                                catch (IndexOutOfRangeException) { }
                            }
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.Gray;
                            ConsoleIO.Write('\n');

                            #endregion
                            
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This bot saves the received messages in a text file.
        /// </summary>

        public class ChatLog : ChatBot
        {
            public enum MessageFilter { AllText, AllMessages, OnlyChat, OnlyWhispers };
            private bool dateandtime;
            private bool saveOther = true;
            private bool saveChat = true;
            private bool savePrivate = true;
            private string logfile;

            /// <summary>
            /// This bot saves the messages received in the specified file, with some filters and date/time tagging.
            /// </summary>
            /// <param name="file">The file to save the log in</param>
            /// <param name="filter">The kind of messages to save</param>
            /// <param name="AddDateAndTime">Add a date and time before each message</param>

            public ChatLog(string file, MessageFilter filter, bool AddDateAndTime)
            {
                dateandtime = AddDateAndTime;
                logfile = file;
                switch (filter)
                {
                    case MessageFilter.AllText:
                        saveOther = true;
                        savePrivate = true;
                        saveChat = true;
                        break;
                    case MessageFilter.AllMessages:
                        saveOther = false;
                        savePrivate = true;
                        saveChat = true;
                        break;
                    case MessageFilter.OnlyChat:
                        saveOther = false;
                        savePrivate = false;
                        saveChat = true;
                        break;
                    case MessageFilter.OnlyWhispers:
                        saveOther = false;
                        savePrivate = true;
                        saveChat = false;
                        break;
                }
            }

            public static MessageFilter str2filter(string filtername)
            {
                switch (filtername.ToLower())
                {
                    case "all": return MessageFilter.AllText;
                    case "messages": return MessageFilter.AllMessages;
                    case "chat": return MessageFilter.OnlyChat;
                    case "private": return MessageFilter.OnlyWhispers;
                    default: return MessageFilter.AllText;
                }
            }

            public override void GetText(string text)
            {
                text = getVerbatim(text);
                string sender = "";
                string message = "";

                if (saveChat && isChatMessage(text, ref message, ref sender))
                {
                    save("Chat " + sender + ": " + message);
                }
                else if (savePrivate && isPrivateMessage(text, ref message, ref sender))
                {
                    save("Private " + sender + ": " + message);
                }
                else if (saveOther)
                {
                    save("Other: " + text);
                }
            }

            private void save(string tosave)
            {
                if (dateandtime)
                {
                    int day = DateTime.Now.Day, month = DateTime.Now.Month;
                    int hour = DateTime.Now.Hour, minute = DateTime.Now.Minute, second = DateTime.Now.Second;

                    string D = day < 10 ? "0" + day : "" + day;
                    string M = month < 10 ? "0" + month : "" + day;
                    string Y = "" + DateTime.Now.Year;

                    string h = hour < 10 ? "0" + hour : "" + hour;
                    string m = minute < 10 ? "0" + minute : "" + minute;
                    string s = second < 10 ? "0" + second : "" + second;

                    tosave = "" + D + '-' + M + '-' + Y + ' ' + h + ':' + m + ':' + s + ' ' + tosave;
                }

                System.IO.FileStream stream = new System.IO.FileStream(logfile, System.IO.FileMode.OpenOrCreate);
                System.IO.StreamWriter writer = new System.IO.StreamWriter(stream);
                stream.Seek(0, System.IO.SeekOrigin.End);
                writer.WriteLine(tosave);
                writer.Dispose();
                stream.Close();
            }
        }

        /// <summary>
        /// This bot automatically re-join the server if kick message contains predefined string (Server is restarting ...)
        /// </summary>

        public class AutoRelog : ChatBot
        {
            private string[] dictionary = new string[0];
            private int attempts;
            private int delay;

            /// <summary>
            /// This bot automatically re-join the server if kick message contains predefined string
            /// </summary>
            /// <param name="DelayBeforeRelog">Delay before re-joining the server (in seconds)</param>
            /// <param name="retries">Number of retries if connection fails (-1 = infinite)</param>

            public AutoRelog(int DelayBeforeRelog, int retries)
            {
                attempts = retries;
                if (attempts == -1) { attempts = int.MaxValue; }
                McTcpClient.AttemptsLeft = attempts;
                delay = DelayBeforeRelog;
                if (delay < 1) { delay = 1; }
            }

            public override void Initialize()
            {
                McTcpClient.AttemptsLeft = attempts;
                if (System.IO.File.Exists(Settings.AutoRelog_KickMessagesFile))
                {
                    dictionary = System.IO.File.ReadAllLines(Settings.AutoRelog_KickMessagesFile);

                    for (int i = 0; i < dictionary.Length; i++)
                    {
                        dictionary[i] = dictionary[i].ToLower();
                    }
                }
                else LogToConsole("File not found: " + Settings.AutoRelog_KickMessagesFile);
            }

            public override bool OnDisconnect(DisconnectReason reason, string message)
            {
                message = getVerbatim(message);
                string comp = message.ToLower();
                foreach (string msg in dictionary)
                {
                    if (comp.Contains(msg))
                    {
                        LogToConsole("Waiting " + delay + " seconds before reconnecting...");
                        System.Threading.Thread.Sleep(delay * 1000);
                        McTcpClient.AttemptsLeft = attempts;
                        ReconnectToTheServer();
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Automatically send login command on servers usign the xAuth plugin
        /// </summary>

        public class xAuth : ChatBot
        {
            private string password;
            private int countdown = 50;

            public xAuth(string pass)
            {
                password = pass;
            }

            public override void Update()
            {
                countdown--;
                if (countdown == 0)
                {
                    SendText("/login " + password);
                    UnloadBot(); //This bot is no more needed.
                }
            }
        }

        /// <summary>
        /// Runs a list of commands
        /// Usage: bot:scripting:filename
        /// Script must be placed in the config directory
        /// </summary>

        public class Scripting : ChatBot
        {
            private string file;
            private string[] lines = new string[0];
            private int sleepticks = 10;
            private int sleepticks_interval = 10;
            private int nextline = 0;
            public Scripting(string filename)
            {
                file = filename;
            }

            public override void Initialize()
            {
                //Load the given file from the startup parameters
                //Automatically look in subfolders and try to add ".txt" file extension
                string[] files = new string[]
                {
                    file,
                    file + ".txt",
                    "scripts\\" + file,
                    "scripts\\" + file + ".txt",
                    "config\\" + file,
                    "config\\" + file + ".txt",
                };

                bool file_found = false;

                foreach (string possible_file in files)
                {
                    if (System.IO.File.Exists(possible_file))
                    {
                        lines = System.IO.File.ReadAllLines(possible_file);
                        file_found = true;
                        break;
                    }
                }

                if (!file_found)
                {
                    LogToConsole("File not found: '" + file + "'");
                    UnloadBot(); //No need to keep the bot active
                }
            }

            public override void Update()
            {
                if (sleepticks > 0) { sleepticks--; }
                else
                {
                    if (nextline < lines.Length) //Is there an instruction left to interpret?
                    {
                        string instruction_line = lines[nextline].Trim(); // Removes all whitespaces at start and end of current line
                        nextline++; //Move the cursor so that the next time the following line will be interpreted
                        sleepticks = sleepticks_interval; //Used to delay next command sending and prevent from beign kicked for spamming

                        if (instruction_line.Length > 1)
                        {
                            if (instruction_line[0] != '#' && instruction_line[0] != '/' && instruction_line[1] != '/')
                            {
                                string instruction_name = instruction_line.Split(' ')[0];
                                switch (instruction_name.ToLower())
                                {
                                    case "send":
                                        SendText(instruction_line.Substring(5, instruction_line.Length - 5));
                                        break;
                                    case "wait":
                                        int ticks = 10;
                                        try
                                        {
                                            ticks = Convert.ToInt32(instruction_line.Substring(5, instruction_line.Length - 5));
                                        }
                                        catch { }
                                        sleepticks = ticks;
                                        break;
                                    case "disconnect":
                                        DisconnectAndExit();
                                        break;
                                    case "exit": //Exit bot & stay connected to the server
                                        UnloadBot();
                                        break;
                                    default:
                                        sleepticks = 0; Update(); //Unknown command : process next line immediately
                                        break;
                                }
                            }
                            else { sleepticks = 0; Update(); } //Comment: process next line immediately
                        }
                    }
                    else
                    {
                        //No more instructions to interpret
                        UnloadBot();
                    }
                }
            }
        }
    }
}
