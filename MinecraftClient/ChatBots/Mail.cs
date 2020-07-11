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
            auto_respawn = true;                                                        // Toggle the internal autorespawn
            //moderator = Settings.Bots_Owners.ToArray();                               // May confuse users at first start, because bot won't answer due to the preconfigured bot owners => mods can be imported!

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
                update_and_send_mails();

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
            string message = "";
            string username = "";

            text = GetVerbatim(text);

            if (IsPrivateMessage(text, ref message, ref username))
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

                if (isModerator(username) || options.moderator.Length == 0) // Delete the safe file of the bot to reset all mods || 2. otion to get the owner as a moderator.
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
                    logged_msg = AddMail(sender, destination, content, true, logged_msg);
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
                Message[] msg_array = getMailsFromFile();
                    
                LogToConsole("Listing all Messages. Performed by: " + sender);
                
                foreach (Message msg in msg_array)
                {
                    LogToConsole(msg.GetSender() + " " + msg.GetDestination() + " " + msg.GetContent() + " " + msg.GetTimeStamp());
                }
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
            /// Reconnect to the server.
            /// </summary>
            if (message.Contains("reconnect"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Reconnecting! \n Performed by: " + sender);
                }
                SaveOptionsToFile();
                ReconnectToTheServer();
            }

            /// <summary>
            /// Manually clear mails older than 30 days.
            /// </summary>
            if (message.Contains("updatemails"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Deleting old mails! \n Performed by: " + sender);
                }
                update_and_send_mails();
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
                if (IsValidName(name))
                {
                    addMod(name);
                    SendPrivateMessage(sender, name + "is now Moderator.");
                    SaveOptionsToFile();

                    if (options.debug_msg)
                    {
                        LogToConsole("Added " + name + " as moderator! \n Performed by: " + sender);
                    }
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

            /// <summary>
            /// List moderators to console.
            /// </summary>
            if (message.Contains("getmoderator"))
            {
                LogToConsole("Moderators are:");

                foreach (string name in options.moderator)
                {
                    LogToConsole(name);
                }

                if (options.debug_msg)
                {
                    LogToConsole("Listed all moderators \n Performed by: " + sender + " Time: " + DateTime.UtcNow + " UTC");
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
                options.daysTosaveMsg = getIntInCommand(message, "daystosavemsg", options.daysTosaveMsg);
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
                options.interval_sendmail = getIntInCommand(message, "intervalsendmail", options.interval_sendmail);
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
                options.maxSavedMails = getIntInCommand(message, "maxsavedmails", options.maxSavedMails);
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
                options.maxSavedMails_Player = getIntInCommand(message, "maxmailsperplayer", options.maxSavedMails_Player);
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
            /// Add the botowners mentioned in the config file to the moderator list.
            /// </summary>
            if (message.Contains("importmoderator"))
            {
                foreach(string mod_name in Settings.Bots_Owners.ToArray())
                {
                    addMod(mod_name);
                    if (options.debug_msg)
                    {
                        LogToConsole(mod_name);
                    }
                }

                if (options.debug_msg)
                {
                    LogToConsole(sender + " added them as moderator. At" + DateTime.UtcNow + " UTC");
                }
            }

            /// <summary>
            /// Resets the timer of the message sender.
            /// </summary>
            if (message.Contains("resettimer"))
            {
                options.timevar_100ms = 0;

                if (options.debug_msg)
                {
                    LogToConsole(sender + " added them as moderator. At" + DateTime.UtcNow + " UTC");
                }
            }

            /// <summary>
            /// List all settings. 
            /// </summary>
            if (message.Contains("getsettings"))
            {
                SendPrivateMessage(sender, "debugmsg: " + Convert.ToString(options.debug_msg) + "; daystosavemsg: " + Convert.ToString(options.daysTosaveMsg) + "; intervalsendmail: " + Convert.ToString(options.interval_sendmail) + "; maxsavedmails: " + Convert.ToString(options.maxSavedMails) + "; maxsavedmails_player: " + Convert.ToString(options.maxSavedMails_Player) + "; messagepath: " + options.path_mail + "; settingspath: " + options.path_setting + "; sutorespawn: " + Convert.ToString(options.auto_respawn));
                LogToConsole("debugmsg: " + Convert.ToString(options.debug_msg) + "; daystosavemsg: " + Convert.ToString(options.daysTosaveMsg) + "; intervalsendmail: " + Convert.ToString(options.interval_sendmail) + "; maxsavedmails: " + Convert.ToString(options.maxSavedMails) + "; maxsavedmails_player: " + Convert.ToString(options.maxSavedMails_Player) + "; messagepath: " + options.path_mail + "; settingspath: " + options.path_setting + "; sutorespawn: " + Convert.ToString(options.auto_respawn));
            }
        }

        /// <summary>
        /// Get the number after a certain word in the message.
        /// </summary>
        public int getIntInCommand(string message, string searched, int currentvalue)
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
                    try
                    {
                        return Int32.Parse(num);
                    }
                    catch (Exception)
                    {
                        return currentvalue;
                    }
                }
            }
            try
            {
                return Int32.Parse(num);
            }
            catch (Exception)
            {
                return currentvalue;
            }
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
            if (!isModerator(name))
            {
                string[] temp = options.moderator;
                options.moderator = new string[options.moderator.Length + 1];

                for (int i = 0; i < temp.Length; i++)
                {
                    options.moderator[i] = temp[i];
                }
                options.moderator[options.moderator.Length - 1] = name;
            }
            else
            {
                if(options.debug_msg)
                {
                    LogToConsole("This name is already in the moderator list.");
                }
            }
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
            catch (Exception) // If, by any reason, the file couldn't be safed, the programm creates a new one and pastes all data in the console for debug use.
            {
                LogToConsole("Something went wrong! Coudln't save cache to file! Replaced the File with an empty one." + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");

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
        /// Test if the sender is in the moderator list.
        /// </summary>
        public bool isModerator(string player)
        {
            if (options.moderator.Contains(player)) { return true; }
            else { return false;  }
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
