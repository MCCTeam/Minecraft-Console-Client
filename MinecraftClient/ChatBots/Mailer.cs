using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// ChatBot for storing and delivering Mails
    /// </summary>
    public class Mailer : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "Mailer";

            public bool Enabled = false;

            public string DatabaseFile = "MailerDatabase.ini";

            public string IgnoreListFile = "MailerIgnoreList.ini";

            public bool PublicInteractions = false;

            public int MaxMailsPerPlayer = 10;

            public int MaxDatabaseSize = 10000;

            public int MailRetentionDays = 30;

            public void OnSettingUpdate()
            {
                DatabaseFile ??= string.Empty;
                IgnoreListFile ??= string.Empty;

                if (!Enabled) return;

                bool checkSuccessed = true;

                if (Config.MaxDatabaseSize <= 0)
                {
                    LogToConsole(BotName, Translations.TryGet("bot.mailer.init_fail.db_size"));
                    checkSuccessed = false;
                }

                if (Config.MaxMailsPerPlayer <= 0)
                {
                    LogToConsole(BotName, Translations.TryGet("bot.mailer.init_fail.max_mails"));
                    checkSuccessed = false;
                }

                if (Config.MailRetentionDays <= 0)
                {
                    LogToConsole(BotName, Translations.TryGet("bot.mailer.init_fail.mail_retention"));
                    checkSuccessed = false;
                }

                if (!checkSuccessed)
                {
                    LogToConsole(BotName, Translations.TryGet("general.bot_unload"));
                    Enabled = false;
                }
            }
        }

        /// <summary>
        /// Holds the list of ignored players
        /// </summary>
        private class IgnoreList : HashSet<string>
        {
            /// <summary>
            /// Read ignore list from file
            /// </summary>
            /// <param name="filePath">Path to the ignore list</param>
            /// <returns>Ignore list</returns>
            public static IgnoreList FromFile(string filePath)
            {
                IgnoreList ignoreList = new();
                foreach (string line in FileMonitor.ReadAllLinesWithRetries(filePath))
                {
                    if (!line.StartsWith("#"))
                    {
                        string entry = line.ToLower();
                        if (!ignoreList.Contains(entry))
                            ignoreList.Add(entry);
                    }
                }
                return ignoreList;
            }

            /// <summary>
            /// Save ignore list to file
            /// </summary>
            /// <param name="filePath">Path to destination file</param>
            public void SaveToFile(string filePath)
            {
                List<string> lines = new();
                lines.Add("#Ignored Players");
                foreach (string player in this)
                    lines.Add(player);
                FileMonitor.WriteAllLinesWithRetries(filePath, lines);
            }
        }

        /// <summary>
        /// Holds the Mail database: a collection of Mails sent from a player to another player
        /// </summary>
        private class MailDatabase : List<Mail>
        {
            /// <summary>
            /// Read mail database from file
            /// </summary>
            /// <param name="filePath">Path to the database</param>
            /// <returns>Mail database</returns>
            public static MailDatabase FromFile(string filePath)
            {
                MailDatabase database = new();
                Dictionary<string, Dictionary<string, string>> iniFileDict = INIFile.ParseFile(FileMonitor.ReadAllLinesWithRetries(filePath));
                foreach (KeyValuePair<string, Dictionary<string, string>> iniSection in iniFileDict)
                {
                    //iniSection.Key is "mailXX" but we don't need it here
                    string sender = iniSection.Value["sender"];
                    string recipient = iniSection.Value["recipient"];
                    string content = iniSection.Value["content"];
                    DateTime timestamp = DateTime.Parse(iniSection.Value["timestamp"]);
                    bool anonymous = INIFile.Str2Bool(iniSection.Value["anonymous"]);
                    database.Add(new Mail(sender, recipient, content, anonymous, timestamp));
                }
                return database;
            }

            /// <summary>
            /// Save mail database to file
            /// </summary>
            /// <param name="filePath">Path to destination file</param>
            public void SaveToFile(string filePath)
            {
                Dictionary<string, Dictionary<string, string>> iniFileDict = new();
                int mailCount = 0;
                foreach (Mail mail in this)
                {
                    mailCount++;
                    Dictionary<string, string> iniSection = new()
                    {
#pragma warning disable format // @formatter:off
                        ["sender"]     =  mail.Sender,
                        ["recipient"]  =  mail.Recipient,
                        ["content"]    =  mail.Content,
                        ["timestamp"]  =  mail.DateSent.ToString(),
                        ["anonymous"]  =  mail.Anonymous.ToString()
#pragma warning restore format // @formatter:on
                    };
                    iniFileDict["mail" + mailCount] = iniSection;
                }
                FileMonitor.WriteAllLinesWithRetries(filePath, INIFile.Generate(iniFileDict, "Mail Database"));
            }
        }

        /// <summary>
        /// Represents a Mail sent from a player to another player
        /// </summary>
        private class Mail
        {
            private readonly string sender;
            private readonly string senderLower;
            private readonly string recipient;
            private readonly string recipientLower;
            private readonly string message;
            private readonly DateTime datesent;
            private bool delivered;
            private readonly bool anonymous;

            public Mail(string sender, string recipient, string message, bool anonymous, DateTime datesent)
            {
                this.sender = sender;
                senderLower = sender.ToLower();
                this.recipient = recipient;
                recipientLower = recipient.ToLower();
                this.message = message;
                this.datesent = datesent;
                delivered = false;
                this.anonymous = anonymous;
            }

            public string Sender { get { return sender; } }
            public string SenderLowercase { get { return senderLower; } }
            public string Recipient { get { return recipient; } }
            public string RecipientLowercase { get { return recipientLower; } }
            public string Content { get { return message; } }
            public DateTime DateSent { get { return datesent; } }
            public bool Delivered => delivered;
            public bool Anonymous { get { return anonymous; } }
            public void SetDelivered() { delivered = true; }

            public override string ToString()
            {
                return String.Format("{0} {1} {2} {3}", Sender, Recipient, Content, DateSent);
            }
        }

        // Internal variables
        private int maxMessageLength = 0;
        private DateTime nextMailSend = DateTime.Now;
        private MailDatabase mailDatabase = new();
        private IgnoreList ignoreList = new();
        private FileMonitor? mailDbFileMonitor;
        private FileMonitor? ignoreListFileMonitor;
        private readonly object readWriteLock = new();

        /// <summary>
        /// Initialization of the Mailer bot
        /// </summary>
        public override void Initialize()
        {
            LogDebugToConsoleTranslated("bot.mailer.init");
            LogDebugToConsoleTranslated("bot.mailer.init.db" + Config.DatabaseFile);
            LogDebugToConsoleTranslated("bot.mailer.init.ignore" + Config.IgnoreListFile);
            LogDebugToConsoleTranslated("bot.mailer.init.public" + Config.PublicInteractions);
            LogDebugToConsoleTranslated("bot.mailer.init.max_mails" + Config.MaxMailsPerPlayer);
            LogDebugToConsoleTranslated("bot.mailer.init.db_size" + Config.MaxDatabaseSize);
            LogDebugToConsoleTranslated("bot.mailer.init.mail_retention" + Config.MailRetentionDays + " days");

            if (!File.Exists(Config.DatabaseFile))
            {
                LogToConsoleTranslated("bot.mailer.create.db", Path.GetFullPath(Config.DatabaseFile));
                new MailDatabase().SaveToFile(Config.DatabaseFile);
            }

            if (!File.Exists(Config.IgnoreListFile))
            {
                LogToConsoleTranslated("bot.mailer.create.ignore", Path.GetFullPath(Config.IgnoreListFile));
                new IgnoreList().SaveToFile(Config.IgnoreListFile);
            }

            lock (readWriteLock)
            {
                LogDebugToConsoleTranslated("bot.mailer.load.db", Path.GetFullPath(Config.DatabaseFile));
                mailDatabase = MailDatabase.FromFile(Config.DatabaseFile);

                LogDebugToConsoleTranslated("bot.mailer.load.ignore", Path.GetFullPath(Config.IgnoreListFile));
                ignoreList = IgnoreList.FromFile(Config.IgnoreListFile);
            }

            //Initialize file monitors. In case the bot needs to unload for some reason in the future, do not forget to .Dispose() them
            mailDbFileMonitor = new FileMonitor(Path.GetDirectoryName(Config.DatabaseFile)!, Path.GetFileName(Config.DatabaseFile), FileMonitorCallback);
            ignoreListFileMonitor = new FileMonitor(Path.GetDirectoryName(Config.IgnoreListFile)!, Path.GetFileName(Config.IgnoreListFile), FileMonitorCallback);

            RegisterChatBotCommand("mailer", Translations.Get("bot.mailer.cmd"), "mailer <getmails|addignored|getignored|removeignored>", ProcessInternalCommand);
        }

        /// <summary>
        /// Standard settings for the bot.
        /// </summary>
        public override void AfterGameJoined()
        {
            maxMessageLength = GetMaxChatMessageLength()
                - 44 // Deduct length of "/ 16CharPlayerName 16CharPlayerName mailed: "
                - Settings.Config.Main.Advanced.PrivateMsgsCmdName.Length; // Deduct length of "tell" command
        }

        /// <summary>
        /// Process chat messages from the server
        /// </summary>
        public override void GetText(string text)
        {
            string message = "";
            string username = "";
            text = GetVerbatim(text);

            if (IsPrivateMessage(text, ref message, ref username) || (Config.PublicInteractions && IsChatMessage(text, ref message, ref username)))
            {
                string usernameLower = username.ToLower();
                if (!ignoreList.Contains(usernameLower))
                {
                    string command = message.Split(' ')[0].ToLower();
                    switch (command)
                    {
                        case "mail":
                        case "tellonym":
                            if (usernameLower != GetUsername().ToLower()
                                && mailDatabase.Count < Config.MaxDatabaseSize
                                && mailDatabase.Where(mail => mail.SenderLowercase == usernameLower).Count() < Config.MaxMailsPerPlayer)
                            {
                                Queue<string> args = new(Command.GetArgs(message));
                                if (args.Count >= 2)
                                {
                                    bool anonymous = (command == "tellonym");
                                    string recipient = args.Dequeue();
                                    message = string.Join(" ", args);

                                    if (IsValidName(recipient))
                                    {
                                        if (message.Length <= maxMessageLength)
                                        {
                                            Mail mail = new(username, recipient, message, anonymous, DateTime.Now);
                                            LogToConsoleTranslated("bot.mailer.saving", mail.ToString());
                                            lock (readWriteLock)
                                            {
                                                mailDatabase.Add(mail);
                                                mailDatabase.SaveToFile(Config.DatabaseFile);
                                            }
                                            SendPrivateMessage(username, "Message saved!");
                                        }
                                        else SendPrivateMessage(username, "Your message cannot be longer than " + maxMessageLength + " characters.");
                                    }
                                    else SendPrivateMessage(username, "Recipient '" + recipient + "' is not a valid player name.");
                                }
                                else SendPrivateMessage(username, "Usage: " + command + " <recipient> <message>");
                            }
                            else SendPrivateMessage(username, "Couldn't save Message. Limit reached!");
                            break;
                    }
                }
                else LogDebugToConsoleTranslated("bot.mailer.user_ignored", username);
            }
        }

        /// <summary>
        /// Called on each MCC tick, around 10 times per second
        /// </summary>
        public override void Update()
        {
            DateTime dateNow = DateTime.Now;
            if (nextMailSend < dateNow)
            {
                LogDebugToConsoleTranslated("bot.mailer.process_mails", DateTime.Now);

                // Process at most 3 mails at a time to avoid spamming. Other mails will be processed on next mail send
                HashSet<string> onlinePlayersLowercase = new(GetOnlinePlayers().Select(name => name.ToLower()));
                foreach (Mail mail in mailDatabase.Where(mail => !mail.Delivered && onlinePlayersLowercase.Contains(mail.RecipientLowercase)).Take(3))
                {
                    string sender = mail.Anonymous ? "Anonymous" : mail.Sender;
                    SendPrivateMessage(mail.Recipient, sender + " mailed: " + mail.Content);
                    mail.SetDelivered();
                    LogDebugToConsoleTranslated("bot.mailer.delivered", mail.ToString());
                }

                lock (readWriteLock)
                {
                    mailDatabase.RemoveAll(mail => mail.Delivered);
                    mailDatabase.RemoveAll(mail => mail.DateSent.AddDays(Config.MailRetentionDays) < DateTime.Now);
                    mailDatabase.SaveToFile(Config.DatabaseFile);
                }

                nextMailSend = dateNow.AddSeconds(10);
            }
        }

        /// <summary>
        /// Called when the Mail Database or Ignore list has changed on disk
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileMonitorCallback(object sender, FileSystemEventArgs e)
        {
            lock (readWriteLock)
            {
                mailDatabase = MailDatabase.FromFile(Config.DatabaseFile);
                ignoreList = IgnoreList.FromFile(Config.IgnoreListFile);
            }
        }

        /// <summary>
        /// Interprets local commands.
        /// </summary>
        private string ProcessInternalCommand(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                string commandName = args[0].ToLower();
                switch (commandName)
                {
                    case "getmails": // Sorry, I (ReinforceZwei) replaced "=" to "-" because it would affect the parsing of translation file (key=value)
                        return Translations.Get("bot.mailer.cmd.getmails", string.Join("\n", mailDatabase));

                    case "getignored":
                        return Translations.Get("bot.mailer.cmd.getignored", string.Join("\n", ignoreList));

                    case "addignored":
                    case "removeignored":
                        if (args.Length > 1 && IsValidName(args[1]))
                        {
                            string username = args[1].ToLower();
                            if (commandName == "addignored")
                            {
                                lock (readWriteLock)
                                {
                                    if (!ignoreList.Contains(username))
                                    {
                                        ignoreList.Add(username);
                                        ignoreList.SaveToFile(Config.IgnoreListFile);
                                    }
                                }
                                return Translations.Get("bot.mailer.cmd.ignore.added", args[1]);
                            }
                            else
                            {
                                lock (readWriteLock)
                                {
                                    if (ignoreList.Contains(username))
                                    {
                                        ignoreList.Remove(username);
                                        ignoreList.SaveToFile(Config.IgnoreListFile);
                                    }
                                }
                                return Translations.Get("bot.mailer.cmd.ignore.removed", args[1]);
                            }
                        }
                        else return Translations.Get("bot.mailer.cmd.ignore.invalid", commandName);
                }
            }
            return Translations.Get("bot.mailer.cmd.help") + ": /help mailer";
        }
    }
}
