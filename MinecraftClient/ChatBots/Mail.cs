using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// All saved options.
    /// </summary>
    [Serializable]
    public class Options
    {
        public string path_mail { get; set; }
        public string path_setting { get; set; }
        public string botname { get; set; }
        public int interval_sendmail { get; set; }
        public int maxSavedMails { get; set; }
        public int maxSavedMails_Player { get; set; }
        public int daysTosaveMsg { get; set; }
        public int timevar_100ms { get; set; }
        public bool debug_msg { get; set; }
        public bool auto_respawn { get; set; }
        public bool allow_sendmail { get; set; }
        public bool allow_receivemail { get; set; }
        public string[] ignored = new string[0];
        public DateTime lastReset { get; set; }
        

        public Options()
        {
            path_mail = AppDomain.CurrentDomain.BaseDirectory + "mails.txt";            // Path where the mail file is saved. You can also apply a normal path like @"C:\Users\SampleUser\Desktop"
            path_setting = AppDomain.CurrentDomain.BaseDirectory + "options.txt";       // Path where the settings are saved
            interval_sendmail = 100;                                                    // Intervall atempting to send mails / do a respawn [in 100 ms] -> eg. 100 * 100ms = 10 sec
            maxSavedMails = 2000;                                                       // How many mails you want to safe
            maxSavedMails_Player = 3;                                                   // How many mails can be sent per player
            daysTosaveMsg = 30;                                                         // After how many days the message should get deleted
            debug_msg = Settings.DebugMessages;                                         // Disable debug Messages for a cleaner console
            auto_respawn = true;                                                        // Toggle the internal autorespawn
            allow_sendmail = true;                                                      // Enable the continious mail sending
            allow_receivemail = true;

            timevar_100ms = 0;
            lastReset = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// The way every mail is safed.
    /// </summary>
    [Serializable]
    public class Message
    {
        string sender;
        string destination;
        string content;
        DateTime timestamp;
        bool delivered;
        bool anonymous;

        public Message(string sender_1, string destination_1, string content_1, bool anonymous_1)
        {
            sender = sender_1;
            destination = destination_1;
            content = content_1;
            timestamp = DateTime.UtcNow;
            delivered = false;
            anonymous = anonymous_1;
        }

        // Obtain Message data.
        public string GetSender()
        {
            return sender;
        }
        public string GetDestination()
        {
            return destination;
        }
        public string GetContent()
        {
            return content;
        }
        public DateTime GetTimeStamp()
        {
            return timestamp;
        }
        public bool isDelivered()
        {
            return delivered;
        }
        public bool isAnonymous()
        {
            return anonymous;
        }
        // Set the message to "delivered" to clear the list later.
        public void setDelivered()
        {
            delivered = true;
        }
    }


    public class Mail : ChatBot
    {
        Message[] logged_msg;
        Options options;

        /// <summary>
        ///  Sets the message an option cache
        /// </summary>
        public override void Initialize()
        {
            logged_msg = new Message[0];
            options = new Options();

            RegisterChatBotCommand("toggleautorespawn", "Enable/Disable autorespawn", toggleAutoRespawn);
            RegisterChatBotCommand("toggledebugmsg", "Enable/Disable debug messages", toggleDebugMSG);
            RegisterChatBotCommand("daystosavemsg", "How long are the unsent mails safed", daysToSaveMessage);
            RegisterChatBotCommand("addignored", "Add a player, the bot ignored", addIgnored);
            RegisterChatBotCommand("removeignored", "Remove a player, the bot ignored", removeIgnored);
            RegisterChatBotCommand("getignored", "Get ignored Players", getIgnored);
            RegisterChatBotCommand("updatemails", "Delete / Send mails", updateMails);
            RegisterChatBotCommand("getmails", "Get all mails from file", getMails);
            RegisterChatBotCommand("getSettings", "See all settings", getSettings);
            RegisterChatBotCommand("resetTimer", "Reset the timer for mail delivering", resetTimer);
            RegisterChatBotCommand("changesettingspath", "Change the path of the setting file relative to the .exe", changeSettingsPath);
            RegisterChatBotCommand("changemailpath", "Change the path of the mail file relative to the .exe", changeMailPath);
            RegisterChatBotCommand("maxmailsperplayer", "How many mails can the individual player send", maxMailsPerPlayer);
            RegisterChatBotCommand("maxsavedmails", "How many mails should be safed at all", maxSavedMails);
            RegisterChatBotCommand("intervalsendmail", "How long should the bot wait until sending mails", intervalSendMail);
            RegisterChatBotCommand("togglemailsending", "Turn the mail sending on / off", toggleMailSending);
            RegisterChatBotCommand("togglemailreceiving", "Turn listening to mail commands on / off", toggleMailReceiving);
        }
        
        /// <summary>
        /// Standard settings for the bot.
        /// </summary>
        public override void AfterGameJoined()
        {
            LogToConsole("Join time: " + DateTime.UtcNow + " UTC.");

            if (!File.Exists(options.path_setting))
            {
                SaveOptionsToFile();
            }
            else
            {
                GetOptionsFromFile();
            }

            options.debug_msg = Settings.DebugMessages;
            options.lastReset = DateTime.UtcNow;
            options.botname = GetUsername();
            update_and_send_mails();
        }

        /// <summary>
        /// Timer for autorespawn and the message deliverer
        /// </summary>
        public override void Update()
        {
            if (options.timevar_100ms == options.interval_sendmail)
            {
                if (options.allow_sendmail)
                {
                    update_and_send_mails();
                }

                if (options.auto_respawn)
                {
                    PerformInternalCommand("respawn");
                }

                options.timevar_100ms = 0;
            }
            options.timevar_100ms++;
        }

        /// <summary>
        /// Listening for Messages.
        /// </summary>
        public override void GetText(string text)
        {
            if (options.allow_receivemail) // Should the bot react to any message?
            {
                string message = "";
                string username = "";

                text = GetVerbatim(text);

                if (IsPrivateMessage(text, ref message, ref username))
                {
                    if (!isIgnored(username))
                    {
                        Message[] msg_array = getMailsFromFile();

                        if (username.ToLower() != options.botname.ToLower() && getSentMessagesByUser(username) < options.maxSavedMails_Player && msg_array.Length < options.maxSavedMails)
                        {
                            message = message.ToLower();
                            cmd_reader(message, username);
                        }
                        else
                        {
                            if (message.Contains("sendmail") || message.Contains("tellonym"))
                            {
                                SendPrivateMessage(username, "Couldn't save Message. Limit reached!");
                            }
                        }
                    }
                    else
                    {
                        if (options.debug_msg)
                        {
                            LogToConsole(username + " is ignored!");
                        }
                    }
                }
            }
            else
            {
                if (options.debug_msg)
                {
                    LogToConsole("Receive Mails is turned off!");
                }
            }
        }

        /// <summary>
        /// Interprets command.
        /// </summary>
        public void cmd_reader(string message, string sender)
        {
            /// <summary>
            /// Send Mails.
            /// </summary>
            if (message.Contains("mail"))
            {
                    string content = "";
                    string destination = "";
                    bool destination_ended = false;

                    for (int i = message.IndexOf("mail") + "mail".Length + 1; i < message.Length; i++) // -> get first letter of the name.
                    {
                        if (message[i].ToString() != " " && !destination_ended)
                        {
                            destination += message[i]; // extract destination
                        }
                        else
                        {
                            destination_ended = true;

                            content += message[i];  // extract message content
                        }
                    }

                    if (IsValidName(sender) && IsValidName(destination) && content != string.Empty)
                    {
                        logged_msg = AddMail(sender, destination, content, false, logged_msg);
                        SendPrivateMessage(sender, "Message saved!");
                    }
                    else
                    {
                        SendPrivateMessage(sender, "Something went wrong!");
                    }
            }

            /// <summary>
            /// Send anonymous mails.
            /// </summary>
            if (message.Contains("tellonym"))
            {
                string content = "";
                string destination = "";
                bool destination_ended = false;

                for (int i = message.IndexOf("tellonym") + "tellonym".Length + 1; i < message.Length; i++) // -> get first letter of the name.
                {
                    if (message[i].ToString() != " " && !destination_ended)
                    {
                        destination += message[i]; // extract destination
                    }
                    else
                    {
                        destination_ended = true;

                        content += message[i];  // extract message content
                    }
                }

                if (IsValidName(sender) && IsValidName(destination) && content != string.Empty)
                {
                    logged_msg = AddMail(sender, destination, content, true, logged_msg);
                }
                else
                {
                    SendPrivateMessage(sender, "Something went wrong!");
                }
            }
        }

        /// <summary>
        /// List ignored players.
        /// </summary>
        public string toggleMailReceiving(string cmd, string[] args)
        {
            if (options.allow_receivemail)
            {
                options.allow_receivemail = false;
                return "Turned mail receiving off.";
            }
            else
            {
                options.allow_receivemail = true;
                return "Turned mail receiving on.";
            }
        }

        /// <summary>
        /// List ignored players.
        /// </summary>
        public string toggleMailSending(string cmd, string[] args)
        {
            if (options.allow_sendmail)
            {
                options.allow_sendmail = false;
                return "Turned mail sending off.";
            }
            else
            {
                options.allow_sendmail = true;
                return "Turned mail sending on.";
            }
        }

        /// <summary>
        /// Change the intervall of respawn and mail sending.
        /// </summary>
        public string intervalSendMail(string cmd, string[] args)
        {
            try
            {
                options.interval_sendmail = Int32.Parse(args[0]);
            }
            catch (Exception)
            {
                return "You answer shall not pass!";
            }

            SaveOptionsToFile();

            return "Changed intervalsendmail to: " + (options.interval_sendmail).ToString();
        }

        /// <summary>
        /// How many mails should be safed in total.
        /// </summary>
        public string maxSavedMails(string cmd, string[] args)
        {
            try
            {
                options.maxSavedMails = Int32.Parse(args[0]);
            }
            catch (Exception)
            {
                return "You answer shall not pass!";
            }

            SaveOptionsToFile();

            return "Changed maxsavedmails to: " + (options.maxSavedMails).ToString();
        }

        /// <summary>
        /// How many mails can an individual player send.
        /// </summary>
        public string maxMailsPerPlayer(string cmd, string[] args)
        {
            try
            {
                options.maxSavedMails_Player = Int32.Parse(args[0]);
            }
            catch (Exception)
            {
                return "You answer shall not pass!";
            }

            SaveOptionsToFile();

            return "Changed maxmailsperplayer to: " + (options.maxSavedMails_Player).ToString();
        }

        /// <summary>
        /// Change the file path of mails.
        /// </summary>
        public string changeMailPath(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                options.path_mail = AppDomain.CurrentDomain.BaseDirectory + args[0];
                SaveOptionsToFile();
                GetOptionsFromFile();

                return "Changed mailpath to: " + (options.path_mail).ToString();
            }
            else
            {
                return "Your path shall not pass!";
            }
        }

        /// <summary>
        /// Change the file path of settings.
        /// </summary>
        public string changeSettingsPath(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                options.path_setting = AppDomain.CurrentDomain.BaseDirectory + args[0];
                SaveOptionsToFile();
                GetOptionsFromFile();

                return "Changed settingsspath to: " + options.path_setting;
            }
            else
            {
                return "Your path shall not pass!";
            }
        }

        /// <summary>
        /// Reset the timer.
        /// </summary>
        public string resetTimer(string cmd, string[] args)
        {
            options.timevar_100ms = 0;

            return "Resetted Timer. At" + DateTime.UtcNow + " UTC";
        }

        /// <summary>
        /// List all settings.
        /// </summary>
        public string getSettings(string cmd, string[] args)
        {
            return "debugmsg: "
                + (options.debug_msg).ToString()
                + "; daystosavemsg: "
                + (options.daysTosaveMsg).ToString()
                + "; intervalsendmail: "
                + (options.interval_sendmail).ToString()
                + "; maxsavedmails: "
                + (options.maxSavedMails).ToString()
                + "; maxsavedmails_player: "
                + (options.maxSavedMails_Player).ToString()
                + "; messagepath: "
                + options.path_mail
                + "; settingspath: "
                + options.path_setting 
                + "; autorespawn: "
                + (options.auto_respawn).ToString()
                + "; togglemailsending: "
                + (options.allow_sendmail).ToString()
                + "; togglemailreceiving: "
                + (options.allow_receivemail).ToString();
        }

        /// <summary>
        /// Get all safed mails.
        /// </summary>
        public string getMails(string cmd, string[] args)
        {
            Message[] msg_array = getMailsFromFile();

            LogToConsole("Listing all Messages.");

            foreach (Message msg in msg_array)
            {
                LogToConsole(msg.GetSender() + " " + msg.GetDestination() + " " + msg.GetContent() + " " + msg.GetTimeStamp());
            }
            return "";
        }

        /// <summary>
        /// Update mails.
        /// </summary>
        public string updateMails(string cmd, string[] args)
        {
            update_and_send_mails();
            return "Sending / Deleting mails!";
        }

        /// <summary>
        /// Add an ignored player to the list.
        /// </summary>
        public string addIgnored(string cmd, string[] args)
        {
            if (args.Length > 0 && IsValidName(args[0]))
            {
                options.ignored = addMember(args[0], options.ignored);
                SaveOptionsToFile();

                return "Added " + args[0] + " as ignored!";
            }
            else
            {
                return "Your name shall not pass!";
            }
        }

        /// <summary>
        /// Remove ignored player from the list.
        /// </summary>
        public string removeIgnored(string cmd, string[] args)
        {
            if (args.Length > 0 && IsValidName(args[0]))
            {
                    options.ignored = removeMember(args[0], options.ignored);
                    SaveOptionsToFile();

                    return "Removed " + args[0] + " as ignored!";
            }
            else
            {
                return "Your name shall not pass!";
            }
        }

        /// <summary>
        /// List ignored players.
        /// </summary>
        public string getIgnored(string cmd, string[] args)
        {
            LogToConsole("Ignored are:");

            foreach (string name in options.ignored)
            {
                LogToConsole(name);
            }

            return "";
        }

        /// <summary>
        /// How many days should your message be safed.
        /// </summary>
        public string daysToSaveMessage(string cmd, string[] args)
        {
            try
            {
                options.daysTosaveMsg = Int32.Parse(args[0]);
            }
            catch (Exception)
            {
                return "You answer shall not pass!";
            }

            SaveOptionsToFile();

            return "Changed daystosavemsg to: " + (options.daysTosaveMsg).ToString();
        }

        /// <summary>
        /// Turns autorespawn on/off.
        /// </summary>
        public string toggleAutoRespawn(string cmd, string[] args)
        {
            if (options.auto_respawn)
            {
                options.auto_respawn = false;
            }
            else
            {
                options.auto_respawn = true;
            }

            SaveOptionsToFile();

            return "Changed autorespawn to: " + (options.auto_respawn).ToString();
        }

        /// <summary>
        /// Turns debug msg on/off.
        /// </summary>
        public string toggleDebugMSG(string cmd, string[] args)
        {
            if (options.debug_msg)
            {
                options.debug_msg = false;
                SaveOptionsToFile();
            }
            else
            {
                options.debug_msg = true;
                SaveOptionsToFile();
            }

            return "Turned debugmsg to " + (options.debug_msg).ToString();
        }

        /// <summary>
        /// Check if player is ignored.
        /// </summary>
        public bool isIgnored(string name)
        {
            if (options.ignored.Contains(name.ToLower())) { return true; }
            else { return false; }
        }

        /// <summary>
        /// Clear the messages in ram.
        /// </summary>
        public void clearLogged_msg()
        {
            logged_msg = new Message[0];
        }

        /// <summary>
        /// Add a player to the given list.
        /// </summary>
        public string[] addMember(string name, string[] name_array)
        {
            
            string[] temp = name_array;
            name_array = new string[name_array.Length + 1];

            for (int i = 0; i < temp.Length; i++)
            {
                name_array[i] = temp[i];
            }
            name_array[name_array.Length - 1] = name.ToLower();
            
            return name_array;
        }

        /// <summary>
        /// Remove a player from the given list.
        /// </summary>
        public string[] removeMember(string name, string[] name_array)
        {
           
            for (int i = 0; i < name_array.Length; i++)
            {
                if (name_array[i] == name)
                {
                    name_array[i] = string.Empty;
                }
            }
            name_array = name_array.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            
            return name_array;
        }

        /// <summary>
        /// Deserialize Save file, sends all mails, clears old mails, adds mails from cache
        /// </summary>
        public void update_and_send_mails()
        {
            Message[] msg_fromFile = getMailsFromFile();                                // Deserialize File.

            LogToConsole("Looking for mails to send: " + DateTime.UtcNow + " UTC");     // Can not be disabled to indicate, that the script is still running. 
            msg_fromFile = DeliverMail(msg_fromFile);                                   //  Try sending all mails in the array.
            logged_msg = DeliverMail(logged_msg);                                       // Sends all messages in chace to minimize the amount of data to safe.

            msg_fromFile = deleteOldMails(msg_fromFile);                                // Clear mails older than 30 days.

            foreach (Message msg in logged_msg)                                         // Compare the mails in cache
            {
                if (!messageExists(msg, msg_fromFile))                                  // If it hasn't been serialized yet, add it.
                {
                    msg_fromFile = AddMail(msg.GetSender(), msg.GetDestination(), msg.GetContent(), msg.isAnonymous(), msg_fromFile);
                }
            }

            saveMailsToFile(msg_fromFile); // Serialize File.
            clearLogged_msg();
        }

        /// <summary>
        /// Check if given mail exists in an array.
        /// </summary>
        public bool messageExists(Message msg_in, Message[] msg_list)
        {
            if (msg_list.Contains(msg_in)) { return true; }
            else { return false; }
        }

        /// <summary>
        /// Serialize mails to binary file.
        /// </summary>
        public void saveMailsToFile(Message[] msg_array)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(options.path_mail, FileMode.Create, FileAccess.Write);

                formatter.Serialize(stream, msg_array);
                stream.Close();

                if (options.debug_msg)
                {
                    LogToConsole("Saved mails to File!" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                }
            }
            catch (Exception) // If, by any reason, the file couldn't be safed:
            {
                try // Try if changing the path fix it!
                {
                    options.path_mail = AppDomain.CurrentDomain.BaseDirectory + "mails.txt";
                    SaveOptionsToFile();

                    LogToConsole("Directory or File not Found! Path changed to:" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");

                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream stream = new FileStream(options.path_mail, FileMode.Create, FileAccess.Write);

                    formatter.Serialize(stream, msg_array);
                    stream.Close();

                    if (options.debug_msg)
                    {
                        LogToConsole("Saved mails to File!" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                    }
                }
                catch (Exception) // If even this can not be done, create a new mail file. (If any strange character can't be safed.)
                {
                    LogToConsole("Something went wrong! Coudln't save cache to file! Creating new file." + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");

                    LogToConsole("Pasting File in Console.");
                    LogToConsole("Sender;   Destination;    Content;    isAnonymous;    Creation Date;");
                    foreach (Message msg in getMailsFromFile())
                    {
                        LogToConsole(msg.GetSender() + "; " + msg.GetDestination() + "; " + msg.GetContent() + "; " + msg.isAnonymous() + "; " + msg.GetTimeStamp());
                    }

                    LogToConsole("Pasting Cache in Console.");
                    foreach (Message msg in logged_msg)
                    {
                        LogToConsole(msg.GetSender() + "; " + msg.GetDestination() + "; " + msg.GetContent() + "; " + msg.isAnonymous() + "; " + msg.GetTimeStamp());
                    }

                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream stream = new FileStream(options.path_mail, FileMode.Create, FileAccess.Write);

                    formatter.Serialize(stream, new Message[0]);
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Get mails from save file.
        /// </summary>
        public Message[] getMailsFromFile()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            
            // Tries to access file and creates a new one, if path doesn't exist, to avoid issues.

            try
            {
                FileStream stream = new FileStream(options.path_mail, FileMode.Open, FileAccess.Read);

                if (options.debug_msg)
                {
                    LogToConsole("Loaded mails from File!" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                }

                Message[] msg_array = (Message[])formatter.Deserialize(stream);
                stream.Close();
                return msg_array;
            }
            catch (Exception)
            {
                options.path_mail = AppDomain.CurrentDomain.BaseDirectory + "mails.txt";
                SaveOptionsToFile();

                LogToConsole("Directory or File not Found! Path changed to:" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                return logged_msg;
            }
        }

        /// <summary>
        /// Serialize settings to binary file.
        /// </summary>
        public void SaveOptionsToFile()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(options.path_setting, FileMode.Create, FileAccess.Write);

            formatter.Serialize(stream, options);
            stream.Close();

            if (options.debug_msg)
            {
                LogToConsole("Saved options to File! " + "Location: " + options.path_setting + " Time: " + DateTime.UtcNow + " UTC");
            }
        }

        /// <summary>
        /// Get settings from save file.
        /// </summary>
        public void GetOptionsFromFile()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            bool error = false;

            // Tries to access file and creates a new one, if path doesn't exist, to avoid issues.

            try
            {
                FileStream stream = new FileStream(options.path_setting, FileMode.Open, FileAccess.Read);
                options = (Options)formatter.Deserialize(stream);
                stream.Close();
            }
            catch (Exception)
            {
                error = true;
                options.path_setting = AppDomain.CurrentDomain.BaseDirectory + "options.txt";
                SaveOptionsToFile();

                LogToConsole("Directory or File not Found! Path changed to:" + " Location: " + options.path_setting + " Time: " + DateTime.UtcNow + " UTC");
            }

            if (options.debug_msg && !error)
            {
                LogToConsole("Loaded options from File! " + "Location: " + options.path_setting + " Time: " + DateTime.UtcNow + " UTC");
            }
        }

        /// <summary>
        /// Add a message to the list.
        /// </summary>
        public Message[] AddMail(string sender, string destination, string content, bool anonymous, Message[] msg_array)
        {
            Message[] tmp = msg_array;
            msg_array = new Message[msg_array.Length + 1];

            for (int i = 0; i < tmp.Length; i++)
            {
                msg_array[i] = tmp[i];
            }

            msg_array[msg_array.Length - 1] = new Message(sender, destination, content, anonymous);

            if (options.debug_msg)
            {
                LogToConsole("Saved message of: " + sender);
            }
            return msg_array;
        }

        /// <summary>
        /// Try to send all messages.
        /// </summary>
        public Message[] DeliverMail(Message[] msg_array)
        {
            foreach(string Player in GetOnlinePlayers())
            {
                foreach (Message msg in msg_array)
                {
                    if (Player.ToLower() == msg.GetDestination().ToLower() && !msg.isDelivered())
                    {
                        if (msg.isAnonymous())
                        {
                            SendPrivateMessage(msg.GetDestination(), "Anonymous mailed: " + msg.GetContent());
                            msg.setDelivered();
                        }
                        else
                        {
                            SendPrivateMessage(msg.GetDestination(), msg.GetSender() + " mailed: " + msg.GetContent());
                            msg.setDelivered();
                        }

                        LogToConsole("Message of " + msg.GetSender() + " delivered to " + msg.GetDestination() + "."); // Can not be disabled to indicate, that the script is still running.

                    }
                }
            }

            msg_array = msg_array.Where(x => !x.isDelivered()).ToArray();
            return msg_array;
        }

        /// <summary>
        /// See how many messages of a user are saved.
        /// </summary>
        public int getSentMessagesByUser(string player)
        {
            int mailcount = 0;
            Message[] msg_array = getMailsFromFile();

            foreach (Message msg in msg_array)
            {
                if (msg.GetSender().ToLower() == player.ToLower())
                {
                    mailcount++;
                }
            }

            return mailcount;
        }

        /// <summary>
        /// Deleting mails older than a month.
        /// </summary>
        public Message[] deleteOldMails(Message[] msg_array)
        {
            for(int i = 0; i < msg_array.Length; i++)
            {
                if ((DateTime.UtcNow - msg_array[i].GetTimeStamp()).Days > options.daysTosaveMsg)
                {
                    msg_array[i].setDelivered();
                }
            }
            msg_array = msg_array.Where(x => !x.isDelivered()).ToArray();

            return msg_array;
        }
    }        
}
