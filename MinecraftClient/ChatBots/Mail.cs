using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
        public int maxCharsInMsg { get; set; }
        public int daysTosaveMsg { get; set; }
        public int timevar_100ms { get; set; }
        public Dictionary<string, bool> bools { get; set; }
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
            maxCharsInMsg = 255;                                                        // How many characters can be in a message (Only content, without command syntax)

            bools = new Dictionary<string, bool>();
            bools.Add("auto_respawn", true);                                            // Toggle the internal autorespawn
            bools.Add("allow_sendmail" , true);                                         // Enable the continious mail sending
            bools.Add("allow_receivemail" , true);                                      // Enable the bot reacting to command
            bools.Add("allow_selfmail", true);                                          // Enable to send mails to yourself (mainly for test reason)
            bools.Add("allow_publicCommands", false);                                   // Should the bot accept commands from normal chat?
            bools.Add("debug_msg", Settings.DebugMessages);                             // Disable debug Messages for a cleaner console

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

            RegisterChatBotCommand("addignored", "Add a player, the bot ignored", addIgnored);
            RegisterChatBotCommand("changemailpath", "Change the path of the mail file relative to the .exe", changeMailPath);
            RegisterChatBotCommand("changesettingspath", "Change the path of the setting file relative to the .exe", changeSettingsPath);
            RegisterChatBotCommand("daystosavemsg", "How long are the unsent mails safed", daysToSaveMessage);
            RegisterChatBotCommand("getignored", "Get ignored Players", getIgnored);
            RegisterChatBotCommand("getmails", "Get all mails from file", getMails);
            RegisterChatBotCommand("getsettings", "See all settings", getSettings);
            RegisterChatBotCommand("intervalsendmail", "How long should the bot wait until sending mails", intervalSendMail);
            RegisterChatBotCommand("maxcharsinmail", "How long can a mail be?", maxCharsInMsg);
            RegisterChatBotCommand("maxmailsperplayer", "How many mails can the individual player send", maxMailsPerPlayer);
            RegisterChatBotCommand("maxsavedmails", "How many mails should be safed at all", maxSavedMails);
            RegisterChatBotCommand("removeignored", "Remove a player, the bot ignored", removeIgnored);
            RegisterChatBotCommand("resettimer", "Reset the timer for mail delivering", resetTimer);
            RegisterChatBotCommand("updatemails", "Delete / Send mails", updateMails);

            RegisterChatBotCommand("setbool", "Your options are: " + string.Join("; ", options.bools.Keys.ToArray()), setBooleans);

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

            options.bools["debug_msg"] = Settings.DebugMessages;
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
                if (options.bools["allow_sendmail"])
                {
                    update_and_send_mails();
                }

                if (options.bools["auto_respawn"])
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
            if (options.bools["allow_receivemail"]) // Should the bot react to any message?
            {
                string message = "";
                string username = "";

                text = GetVerbatim(text);

                if (IsPrivateMessage(text, ref message, ref username) || (IsChatMessage(text, ref message, ref username) && options.bools["allow_publicCommands"]))
                {
                    if (!isIgnored(username))
                    {
                        Message[] msg_array = getMailsFromFile();

                        if (username.ToLower() != options.botname.ToLower() && getSentMessagesByUser(username) < options.maxSavedMails_Player && msg_array.Length < options.maxSavedMails)
                        {
                            if (message.ToLower().Contains("mail"))                     // IS it "mail" command
                            {
                                command_interpreter("mail", message, username);
                            }
                            else if (message.ToLower().Contains("tellonym"))            // IS it "tellonym" command
                            {
                                command_interpreter("tellonym", message, username);
                            }
                        }
                        else if (message.Contains("mail") || message.Contains("tellonym"))
                        {
                            SendPrivateMessage(username, "Couldn't save Message. Limit reached!");
                        }
                    }
                    else if (options.bools["debug_msg"])
                    {
                        LogToConsole(username + " is ignored!");
                    }
                }
            }
            else if(options.bools["debug_msg"])
            {
                LogToConsole("Receive Mails is turned off!");
            }
        }

        /// <summary>
        /// Reads & executes the command.
        /// </summary>
        public void command_interpreter(string command, string message, string sender)
        {
            string content = "";
            string destination = "";


            for (int i = message.ToLower().IndexOf(command) + command.Length + 1; i < message.Length; i++) // -> get first letter of the name.
            {
                if (message[i].ToString() != " ")
                {
                    destination += message[i]; // extract destination
                }
                else
                {
                    string temp_content = message.Substring(i + 1);

                    if (temp_content.Length <= options.maxCharsInMsg) // Is the content length within the given range of the host
                    {
                        content = temp_content;  // extract message content
                        break;
                    }
                    else
                    {
                        content = string.Empty;
                        break;
                    }
                }
            }

            if (IsValidName(sender) && IsValidName(destination) && content != string.Empty)
            {
                if (destination.ToLower() != sender.ToLower() || options.bools["allow_selfmail"])
                {
                    if (command == "mail")
                    {
                        logged_msg = AddMail(sender, destination, content, false, logged_msg);
                        SendPrivateMessage(sender, "Message saved!");
                    }
                    else
                    {
                        logged_msg = AddMail(sender, destination, content, true, logged_msg);
                        SendPrivateMessage(sender, "Message saved!");
                    }
                }
            }
            else
            {
                SendPrivateMessage(sender, "Something went wrong! Max characters: " + options.maxCharsInMsg);
            }
        }

        /// <summary>
        /// Toggles all commands.
        /// </summary>
        public string setBooleans(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                try
                {
                    if (args.Length > 1)                                            // Check if a vlaue is given
                    { options.bools[args[0]] = bool.Parse(args[1].ToLower()); }     // If yes, try to set the bool to this value
                    else { options.bools[args[0]] = !options.bools[args[0]]; }      // Otherwise toggle.
                    
                    SaveOptionsToFile();
                    return "Changed " + args[0] + " to: " + (options.bools[args[0]]).ToString();
                }
                catch (Exception)
                {
                    return "This option does not exist!";
                }
            }
            else
            {
                return "Missing argument!";
            }
        }

        /// <summary>
        /// Change the intervall of respawn and mail sending.
        /// </summary>
        public string maxCharsInMsg(string cmd, string[] args)
        {
            try
            {
                options.maxCharsInMsg = Int32.Parse(args[0]);
            }
            catch (Exception)
            {
                return "You answer shall not pass!";
            }

            SaveOptionsToFile();
            return "Changed maximum amounts of characters in a mail to: " + (options.maxCharsInMsg).ToString();
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
            return "\n debug_msg: "
                + (options.bools["debug_msg"]).ToString()
                + ";\n daystosavemsg: "
                + (options.daysTosaveMsg).ToString()
                + ";\n intervalsendmail: "
                + (options.interval_sendmail).ToString()
                + ";\n maxsavedmails: "
                + (options.maxSavedMails).ToString()
                + ";\n maxsavedmails_player: "
                + (options.maxSavedMails_Player).ToString()
                + ";\n messagepath: "
                + options.path_mail
                + ";\n settingspath: "
                + options.path_setting 
                + ";\n auto_respawn: "
                + (options.bools["auto_respawn"]).ToString()
                + ";\n allow_sendmail: "
                + (options.bools["allow_sendmail"]).ToString()
                + ";\n allow_receivemail: "
                + (options.bools["allow_receivemail"]).ToString()
                + ";\n maxcharsinmail: "
                + (options.maxCharsInMsg).ToString()
                + ";\n allow_selfmail: "
                + (options.bools["allow_selfmail"]).ToString()
                + ";\n allow_publicCommands: "
                + (options.bools["allow_publicCommands"]).ToString();
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

                if (options.bools["debug_msg"])
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

                    if (options.bools["debug_msg"])
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

                if (options.bools["debug_msg"])
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

            if (options.bools["debug_msg"])
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

            if (options.bools["debug_msg"] && !error)
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

            if (options.bools["debug_msg"])
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
