using System;
using System.Linq;
using System.Data;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// All saved options.
    /// </summary>
    [Serializable]
    class Options
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
        public string[] moderator = new string[0];
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
            //moderator = Settings.Bots_Owners.ToArray();                               // May confuse users at first start, because bot won't answer due to the preconfigured bot owners
            auto_respawn = true;

            timevar_100ms = 0;
            lastReset = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// The way every mail is safed.
    /// </summary>
    [Serializable]
    class Message
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
        }
        

        /// <summary>
        /// Standard settings for the bot.
        /// </summary>
        public override void AfterGameJoined()
        {
            if (!File.Exists(options.path_setting))
            {
                SaveOptionsToFile();
            }
            else
            {
                GetOptionsFromFile();
            }

            if (!File.Exists(options.path_mail))
            {
                SaveMailsToFile();
            }
            options.debug_msg = Settings.DebugMessages;

            deleteOldMails();
            options.lastReset = DateTime.UtcNow;
            options.botname = GetUsername();
        }

        /// <summary>
        /// Timer for autorespawn and the message deliverer
        /// </summary>
        public override void Update()
        {
            if (options.timevar_100ms == options.interval_sendmail)
            {
                DeliverMail();

                if (options.auto_respawn)
                {
                    PerformInternalCommand("respawn");
                }

                if ((DateTime.Now - options.lastReset).TotalDays > options.daysTosaveMsg)
                {
                    deleteOldMails();
                    options.lastReset = DateTime.UtcNow;
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
            string message = "";
            string username = "";

            text = GetVerbatim(text);

            if (IsPrivateMessage(text, ref message, ref username))
            {
                if (username.ToLower() != options.botname.ToLower() && getSentMessagesByUser(username) < options.maxSavedMails_Player && logged_msg.Length < options.maxSavedMails)
                {
                    message = message.ToLower();
                    cmd_reader(message, username);
                }
                else
                {
                    SendPrivateMessage(username, "Couldn't save Message. Limit reached!");
                }

                if (isMessageFromMod(username) || options.moderator.Length == 0) // Delete the safe file of the bot to reset all mods || 2. otion to get the owner as a moderator.
                {                  
                    mod_Commands(message, username);                    
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
            if (message.Contains("sendmail"))
            {
                    string content = "";
                    string destination = "";
                    bool destination_ended = false;

                    for (int i = message.IndexOf("sendmail") + "sendmail".Length + 1; i < message.Length; i++) // -> get first letter of the name.
                    {
                        if (message[i] != Convert.ToChar(" ") && !destination_ended)
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
                        AddMail(sender, destination, content, false);
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
                    if (message[i] != Convert.ToChar(" ") && !destination_ended)
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
                    AddMail("Anonymous", destination, content, true);
                }
                else
                {
                    SendPrivateMessage(sender, "Something went wrong!");
                }
            }
        }

        /// <summary>
        /// Mod only commands.
        /// </summary>
        private void mod_Commands(string message, string sender)
        {

            ////////////////////////////////////////////////////////////////////
            // These commands can be exploited easily and may cause huge damage.
            ////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Send all mails in the console.
            /// </summary>
            if (message.Contains("getmails"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Listing all Messages. \n Performed by: " + sender);
                }
                GetMailsFromFile();
                foreach (Message m in logged_msg)
                {
                    LogToConsole(m.GetSender() + " " + m.GetDestination() + " " + m.GetContent() + " " + Convert.ToString(m.GetTimeStamp()));
                }

                clearLogged_msg();
            }

            // Only uncomment for testing reasons: !! HUGE DAMAGE !!

            /// <summary>
            /// Clear the mail file.
            /// </summary>
            /*
            if (message.Contains("clearmails"))
            {
                LogToConsole("Clearing Messages. Executed by: " + sender + "At: " + DateTime.UtcNow); // can't be disabled for security.  
                clearSavedMails();
            }
            */

            /// <summary>
            /// Let the bot respawn manually.
            /// </summary>
            if (message.Contains("respawn"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Respawning! \n Performed by: " + sender);
                }
                PerformInternalCommand("respawn");
            }

            /// <summary>
            /// Deliver mails manually.
            /// </summary>
            if (message.Contains("deliver"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Sending Mails! \n Performed by: " + sender);
                }
                DeliverMail();
            }

            /// <summary>
            /// Reconnect to the server.
            /// </summary>
            if (message.Contains("reconnect"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Reconnecting! \n Performed by: " + sender);
                }
                SaveMailsToFile();
                SaveOptionsToFile();
                ReconnectToTheServer();
            }

            /// <summary>
            /// Manually clear mails older than 30 days.
            /// </summary>
            if (message.Contains("deleteoldmails"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Deleting old mails! \n Performed by: " + sender);
                }
                deleteOldMails();
                SaveMailsToFile();
            }

            /// <summary>
            /// Add a moderator.
            /// </summary>
            if (message.Contains("addmod"))
            {
                string name = "";

                for (int i = message.IndexOf("addMod") + 7; i < message.Length; i++)
                {
                    if (message[i] != Convert.ToChar(" "))
                    {
                        name += message[i];
                    }
                    else
                    {
                        break;
                    }
                }

                addMod(name);
                SendPrivateMessage(sender, name + "is now Moderator.");
                SaveOptionsToFile();

                if (options.debug_msg)
                {
                    LogToConsole("Added " + name + " as moderator! \n Performed by: " + sender);
                }
            }

            /// <summary>
            /// Remove a moderator.
            /// </summary>
            if (message.Contains("removemod"))
            {
                string name = "";


                for (int i = message.IndexOf("addMod") + 7; i < message.Length; i++)
                {
                    if (message[i] != Convert.ToChar(" "))
                    {
                        name += message[i];
                    }
                    else
                    {
                        break;
                    }
                }

                removeMod(name);
                SendPrivateMessage(sender, name + "is no Moderator anymmore.");
                SaveOptionsToFile();

                if (options.debug_msg)
                {
                    LogToConsole("Removed " + name + " as moderator! \n Performed by: " + sender);
                }
            }

            //////////////////////////////
            // Change options through mc chat
            //////////////////////////////

            /// <summary>
            /// Toggles if debug messages are sent.
            /// </summary>
            if (message.Contains("toggledebug"))
            {
                if (options.debug_msg)
                {
                    options.debug_msg = false;
                    if (options.debug_msg)
                    {
                        LogToConsole(sender + ": Turned Console Log off!");
                    }
                }
                else
                {
                    options.debug_msg = true;
                    if (options.debug_msg)
                    {
                        LogToConsole(sender + ": Turned Console Log off!");
                    }
                }
                SendPrivateMessage(sender, "Settings changed!");
                SaveOptionsToFile();
            }

            /// <summary>
            /// How many days until the mails are deleted?
            /// </summary>
            if (message.Contains("daystosavemsg"))
            {
                options.daysTosaveMsg = getIntInCommand(message, "daystosavemsg");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed daystosavemsg to: " + Convert.ToString(options.daysTosaveMsg));
                }
            }

            /// <summary>
            /// Frequency of the bot trying to deliver mails.
            /// </summary>
            if (message.Contains("intervalsendmail"))
            {
                options.interval_sendmail = getIntInCommand(message, "intervalsendmail");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed intervalsendmail to: " + Convert.ToString(options.interval_sendmail));
                }
            }

            /// <summary>
            /// Maximum of mails that are safed.
            /// </summary>
            if (message.Contains("maxsavedmails"))
            {
                options.maxSavedMails = getIntInCommand(message, "maxsavedmails");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed maxsavedmails to: " + Convert.ToString(options.maxSavedMails));
                }
            }

            /// <summary>
            /// Maximum of mails that are safed per player.
            /// </summary>
            if (message.Contains("maxmailsperplayer"))
            {
                options.maxSavedMails_Player = getIntInCommand(message, "maxmailsperplayer");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed maxmailsperplayer to: " + Convert.ToString(options.maxSavedMails_Player));
                }
            }

            /// <summary>
            /// Change the mail path to:
            /// application-path\ + entry 
            /// </summary>
            if (message.Contains("changemailpath"))
            {
                string path = "";
                for (int i = message.IndexOf("changemailpath") + "changemailpath".Length + 1; i < message.Length; i++)
                {
                    if (message[i] != Convert.ToChar(" "))
                    {
                        path += message[i];
                    }
                    else
                    {
                        break;
                    }
                }
                options.path_mail = AppDomain.CurrentDomain.BaseDirectory + path;
                SendPrivateMessage(sender, "Settings changed!");
                SaveOptionsToFile();
                GetOptionsFromFile();

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed mailpath to: " + Convert.ToString(options.path_mail));
                }

            }

            /// <summary>
            /// Change the mail path to:
            /// application-path\ + entry 
            /// </summary>
            if (message.Contains("changesettingspath"))
            {
                string path = "";
                for (int i = message.IndexOf("changesettingspath") + "changesettingspath".Length + 1; i < message.Length; i++)
                {
                    if (message[i] != Convert.ToChar(" "))
                    {
                        path += message[i];
                    }
                    else
                    {
                        break;
                    }
                }
                options.path_setting = AppDomain.CurrentDomain.BaseDirectory + path;
                SendPrivateMessage(sender, "Settings changed!");
                SaveOptionsToFile();
                GetOptionsFromFile();

                if(options.debug_msg)
                {
                    LogToConsole(sender + " changed settingsspath to: " + Convert.ToString(options.path_setting));
                }

            }

            /// <summary>
            /// List all settings. 
            /// </summary>
            if (message.Contains("toggleautorespawn"))
            {
                if (options.auto_respawn)
                {
                    options.auto_respawn = false;
                }
                else
                {
                    options.auto_respawn = true;
                }

                SendPrivateMessage(sender, "Settings changed!");
                SaveOptionsToFile();

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed maxsavedmails to: " + Convert.ToString(options.auto_respawn));
                }
            }

            /// <summary>
            /// List all settings. 
            /// </summary>
            if (message.Contains("listsettings"))
            {
                SendPrivateMessage(sender, "debugmsg: " + Convert.ToString(options.debug_msg) + "; daystosavemsg: " + Convert.ToString(options.daysTosaveMsg) + "; intervalsendmail: " + Convert.ToString(options.interval_sendmail) + "; maxsavedmails: " + Convert.ToString(options.maxSavedMails) + "; maxsavedmails_player: " + Convert.ToString(options.maxSavedMails_Player) + "; messagepath: " + options.path_mail + "; settingspath: " + options.path_setting + "; sutorespawn: " + Convert.ToString(options.auto_respawn));
            }
        }

        /// <summary>
        /// Get the number after a certain word in the message.
        /// </summary>
        public int getIntInCommand(string message, string searched)
        {
            string num = "";
            for (int i = message.IndexOf(searched) + searched.Length + 1; i < message.Length; i++)
            {
                if (message[i] != Convert.ToChar(" "))
                {
                    num += message[i];
                }
                else
                {
                    return Int32.Parse(num);
                }
            }
            return Int32.Parse(num);
        }

        /// <summary>
        /// Clear the safe File.
        /// </summary>
        public void clearSavedMails()
        {
            clearLogged_msg();
            SaveMailsToFile();
        }

        /// <summary>
        /// Clear the messages in ram.
        /// </summary>
        public void clearLogged_msg()
        {
            logged_msg = new Message[0];
        }

        /// <summary>
        /// Add a player who can moderate the bot.
        /// </summary>
        public void addMod(string name)
        {
            string[] temp = options.moderator;
            options.moderator = new string[options.moderator.Length + 1];

            for (int i = 0; i < temp.Length; i++)
            {
                options.moderator[i] = temp[i];
            }
            options.moderator[options.moderator.Length - 1] = name;
        }

        /// <summary>
        /// Remove a player from the moderator list.
        /// </summary>
        public void removeMod(string name)
        {

            for (int i = 0; i < options.moderator.Length; i++)
            {
                if (options.moderator[i] == name)
                {
                    options.moderator[i] = string.Empty;
                }
            }
            options.moderator = options.moderator.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        /// <summary>
        /// Serialize mails to binary file.
        /// </summary>
        public void SaveMailsToFile()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(options.path_mail, FileMode.Create, FileAccess.Write);

            formatter.Serialize(stream, logged_msg);
            stream.Close();

            if (options.debug_msg)
            {
                LogToConsole("Saved mails to File!"  + " Location: " + options.path_mail + " Time: " + Convert.ToString(DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Get mails from save file.
        /// </summary>
        public void GetMailsFromFile()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            bool error = false;

            // Tries to access file and creates a new one, if path doesn't exist, to avoid issues.

            try
            {
                FileStream stream = new FileStream(options.path_mail, FileMode.Open, FileAccess.Read);
                logged_msg = (Message[])formatter.Deserialize(stream);
                stream.Close();
            }
            catch (Exception)
            {
                error = true;
                options.path_mail = AppDomain.CurrentDomain.BaseDirectory + "mails.txt";
                SaveMailsToFile();
                SaveOptionsToFile();

                LogToConsole("Directory or File not Found! Path changed to:" + " Location: " + options.path_mail + " Time: " + Convert.ToString(DateTime.UtcNow));
            }
           

            if (options.debug_msg && !error)
            {
                LogToConsole("Loaded mails from File!" + " Location: " + options.path_mail + " Time: " + Convert.ToString(DateTime.UtcNow));
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
                LogToConsole("Saved options to File! " + "Location: " + options.path_setting + " Time: " + Convert.ToString(DateTime.UtcNow));
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

                LogToConsole("Directory or File not Found! Path changed to:" + " Location: " + options.path_setting + " Time: " + Convert.ToString(DateTime.UtcNow));
            }

            if (options.debug_msg && !error)
            {
                LogToConsole("Loaded options from File! " + "Location: " + options.path_setting + " Time: " + Convert.ToString(DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Add a message to the list.
        /// </summary>
        public void AddMail(string sender, string destination, string content, bool anonymous)
        {
            GetMailsFromFile();

            Message[] tmp = logged_msg;
            logged_msg = new Message[logged_msg.Length + 1];

            for (int i = 0; i < tmp.Length; i++)
            {
                logged_msg[i] = tmp[i];
            }

            logged_msg[logged_msg.Length - 1] = new Message(sender, destination, content, anonymous);

            SaveMailsToFile();
            SendPrivateMessage(sender, "Message saved!");
            if (options.debug_msg)
            {
                LogToConsole("Saved message of: " + sender);
            }
        }

        /// <summary>
        /// Try to send all messages.
        /// </summary>
        public void DeliverMail()
        {
            LogToConsole("Looking for mails to send: " + DateTime.UtcNow); // Can not be disabled to indicate, that the script is still running. 
            GetMailsFromFile();

            foreach(string Player in GetOnlinePlayers())
            {
                foreach (Message msg in logged_msg)
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

            logged_msg = logged_msg.Where(x => !x.isDelivered()).ToArray();
            SaveMailsToFile();
            clearLogged_msg();
        }

        /// <summary>
        /// See how many messages of a user are saved.
        /// </summary>
        public int getSentMessagesByUser(string player)
        {
            GetMailsFromFile();
            int mailcount = 0;

            foreach (Message msg in logged_msg)
            {
                if (msg.GetSender().ToLower() == player.ToLower())
                {
                    mailcount++;
                }
            }
            logged_msg = new Message[0];
            return mailcount;
        }

        /// <summary>
        /// Test if the sender is in the moderator list.
        /// </summary>
        public bool isMessageFromMod(string player)
        {
            foreach (string mod in options.moderator)
            {
                if (mod.ToLower() == player.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Deleting mails older than a month.
        /// </summary>
        public void deleteOldMails()
        {
            GetMailsFromFile();

            for(int i = 0; i < logged_msg.Length; i++)
            {
                if ((DateTime.UtcNow - logged_msg[i].GetTimeStamp()).TotalDays > options.daysTosaveMsg)
                {
                    logged_msg[i].setDelivered();
                }
            }
            logged_msg = logged_msg.Where(x => !x.isDelivered()).ToArray();
            SaveMailsToFile();
            clearLogged_msg();
        }
    }        
}
