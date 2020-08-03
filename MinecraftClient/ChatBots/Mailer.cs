using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// ChatBot for storing and delivering Mails
    /// </summary>
    public class Mailer : ChatBot
    {
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
                IgnoreList ignoreList = new IgnoreList();
                foreach (string line in File.ReadAllLines(filePath))
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
                List<string> lines = new List<string>();
                lines.Add("#Ignored Players");
                foreach (string player in this)
                    lines.Add(player);
                File.WriteAllLines(filePath, lines);
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
                MailDatabase database = new MailDatabase();
                Dictionary<string, Dictionary<string, string>> iniFileDict = INIFile.ParseFile(filePath);
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
                Dictionary<string, Dictionary<string, string>> iniFileDict = new Dictionary<string, Dictionary<string, string>>();
                int mailCount = 0;
                foreach (Mail mail in this)
                {
                    mailCount++;
                    Dictionary<string, string> iniSection = new Dictionary<string, string>();
                    iniSection["sender"] = mail.Sender;
                    iniSection["recipient"] = mail.Recipient;
                    iniSection["content"] = mail.Content;
                    iniSection["timestamp"] = mail.DateSent.ToString();
                    iniSection["anonymous"] = mail.Anonymous.ToString();
                    iniFileDict["mail" + mailCount] = iniSection;
                }
                INIFile.WriteFile(filePath, iniFileDict, "Mail Database");
            }
        }

        /// <summary>
        /// Represents a Mail sent from a player to another player
        /// </summary>
        private class Mail
        {
            private string sender;
            private string senderLower;
            private string recipient;
            private string message;
            private DateTime datesent;
            private bool delivered;
            private bool anonymous;

            public Mail(string sender, string recipient, string message, bool anonymous, DateTime datesent)
            {
                this.sender = sender;
                this.senderLower = sender.ToLower();
                this.recipient = recipient;
                this.message = message;
                this.datesent = datesent;
                this.delivered = false;
                this.anonymous = anonymous;
            }

            public string Sender { get { return sender; } }
            public string SenderLowercase { get { return senderLower; } }
            public string Recipient { get { return recipient; } }
            public string Content { get { return message; } }
            public DateTime DateSent { get { return datesent; } }
            public bool Delivered { get { return delivered; } }
            public bool Anonymous { get { return anonymous; } }
            public void setDelivered() { delivered = true; }

            public override string ToString()
            {
                return String.Format("{0} {1} {2} {3}", Sender, Recipient, Content, DateSent);
            }
        }

        // Internal variables
        private int maxMessageLength = 0;
        private DateTime nextMailSend = DateTime.Now;
        private MailDatabase mailDatabase = new MailDatabase();
        private IgnoreList ignoreList = new IgnoreList();

        /// <summary>
        /// Initialization of the Mailer bot
        /// </summary>
        public override void Initialize()
        {
            LogDebugToConsole("Initializing Mailer with settings:");
            LogDebugToConsole(" - Database File: " + Settings.Mailer_DatabaseFile);
            LogDebugToConsole(" - Ignore List: " + Settings.Mailer_IgnoreListFile);
            LogDebugToConsole(" - Public Interactions: " + Settings.Mailer_PublicInteractions);
            LogDebugToConsole(" - Max Mails per Player: " + Settings.Mailer_MaxMailsPerPlayer);
            LogDebugToConsole(" - Max Database Size: " + Settings.Mailer_MaxDatabaseSize);
            LogDebugToConsole(" - Mail Retention: " + Settings.Mailer_MailRetentionDays + " days");

            if (Settings.Mailer_MaxDatabaseSize <= 0)
            {
                LogToConsole("Cannot enable Mailer: Max Database Size must be greater than zero. Please review the settings.");
                UnloadBot();
                return;
            }

            if (Settings.Mailer_MaxMailsPerPlayer <= 0)
            {
                LogToConsole("Cannot enable Mailer: Max Mails per Player must be greater than zero. Please review the settings.");
                UnloadBot();
                return;
            }

            if (Settings.Mailer_MailRetentionDays <= 0)
            {
                LogToConsole("Cannot enable Mailer: Mail Retention must be greater than zero. Please review the settings.");
                UnloadBot();
                return;
            }

            if (!File.Exists(Settings.Mailer_DatabaseFile))
            {
                LogToConsole("Creating new database file: " + Path.GetFullPath(Settings.Mailer_DatabaseFile));
                new MailDatabase().SaveToFile(Settings.Mailer_DatabaseFile);
            }

            if (!File.Exists(Settings.Mailer_IgnoreListFile))
            {
                LogToConsole("Creating new ignore list: " + Path.GetFullPath(Settings.Mailer_IgnoreListFile));
                new IgnoreList().SaveToFile(Settings.Mailer_IgnoreListFile);
            }

            LogDebugToConsole("Loading database file: " + Path.GetFullPath(Settings.Mailer_DatabaseFile));
            mailDatabase = MailDatabase.FromFile(Settings.Mailer_DatabaseFile);

            LogDebugToConsole("Loading ignore list: " + Path.GetFullPath(Settings.Mailer_IgnoreListFile));
            ignoreList = IgnoreList.FromFile(Settings.Mailer_IgnoreListFile);

            RegisterChatBotCommand("mailer", "Subcommands: getmails, addignored, getignored, removeignored", ProcessInternalCommand);
        }

        /// <summary>
        /// Standard settings for the bot.
        /// </summary>
        public override void AfterGameJoined()
        {
            maxMessageLength = GetMaxChatMessageLength()
                - 44 // Deduct length of "/ 16CharPlayerName 16CharPlayerName mailed: "
                - Settings.PrivateMsgsCmdName.Length; // Deduct length of "tell" command
        }

        /// <summary>
        /// Process chat messages from the server
        /// </summary>
        public override void GetText(string text)
        {
            string message = "";
            string username = "";
            text = GetVerbatim(text);

            if (IsPrivateMessage(text, ref message, ref username) || (Settings.Mailer_PublicInteractions && IsChatMessage(text, ref message, ref username)))
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
                                && mailDatabase.Count < Settings.Mailer_MaxDatabaseSize
                                && mailDatabase.Where(mail => mail.SenderLowercase == usernameLower).Count() < Settings.Mailer_MaxMailsPerPlayer)
                            {
                                Queue<string> args = new Queue<string>(Command.getArgs(message));
                                if (args.Count >= 2)
                                {
                                    bool anonymous = (command == "tellonym");
                                    string recipient = args.Dequeue();
                                    message = string.Join(" ", args);

                                    if (IsValidName(recipient))
                                    {
                                        if (message.Length <= maxMessageLength)
                                        {
                                            Mail mail = new Mail(username, recipient, message, anonymous, DateTime.Now);
                                            LogToConsole("Saving message: " + mail.ToString());
                                            mailDatabase.Add(mail);
                                            mailDatabase.SaveToFile(Settings.Mailer_DatabaseFile);
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
                else LogDebugToConsole(username + " is ignored!");
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
                LogDebugToConsole("Looking for mails to send @ " + DateTime.Now);

                // Reload mail and ignore list database in case several instances are sharing the same database
                mailDatabase = MailDatabase.FromFile(Settings.Mailer_DatabaseFile);
                ignoreList = IgnoreList.FromFile(Settings.Mailer_IgnoreListFile);

                // Process at most 3 mails at a time to avoid spamming. Other mails will be processed on next mail send
                HashSet<string> onlinePlayer = new HashSet<string>(GetOnlinePlayers());
                foreach (Mail mail in mailDatabase.Where(mail => !mail.Delivered && onlinePlayer.Contains(mail.Recipient)).Take(3))
                {
                    string sender = mail.Anonymous ? "Anonymous" : mail.Sender;
                    SendPrivateMessage(mail.Recipient, sender + " mailed: " + mail.Content);
                    mail.setDelivered();
                    LogDebugToConsole("Delivered: " + mail.ToString());
                }

                mailDatabase.RemoveAll(mail => mail.Delivered);
                mailDatabase.RemoveAll(mail => mail.DateSent.AddDays(Settings.Mailer_MailRetentionDays) < DateTime.Now);
                mailDatabase.SaveToFile(Settings.Mailer_DatabaseFile);

                nextMailSend = dateNow.AddSeconds(10);
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
                    case "getmails":
                        return "== Mails in database ==\n" + string.Join("\n", mailDatabase);

                    case "getignored":
                        return "== Ignore list ==\n" + string.Join("\n", ignoreList);

                    case "addignored":
                    case "removeignored":
                        if (args.Length > 1 && IsValidName(args[1]))
                        {
                            string username = args[1].ToLower();
                            if (commandName == "addignored")
                            {
                                if (!ignoreList.Contains(username))
                                {
                                    ignoreList.Add(username);
                                    ignoreList.SaveToFile(Settings.Mailer_IgnoreListFile);
                                }
                                return "Added " + args[1] + " to the ignore list!";
                            }
                            else
                            {
                                if (ignoreList.Contains(username))
                                {
                                    ignoreList.Remove(username);
                                    ignoreList.SaveToFile(Settings.Mailer_IgnoreListFile);
                                }
                                return "Removed " + args[1] + " from the ignore list!";
                            }
                        }
                        else return "Missing or invalid name. Usage: " + commandName + " <username>";
                }
            }
            return "See usage: /help mailer";
        }
    }
}
