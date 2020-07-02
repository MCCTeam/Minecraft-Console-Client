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
        public bool debug_msg { get; set; }
        public string[] moderator = new string[0];
        public DateTime lastReset { get; set; }
        public int timevar_100ms { get; set; }

        public Options()
        {
            path_mail = Path.GetFullPath(@"\mails.txt");             // Path where the mail file is saved. You can also apply a normal path like @"C:\Users\SampleUser\Desktop"
            path_setting = Path.GetFullPath(@"\options.txt");        // Path where the settings are saved
            interval_sendmail = 100;                                 // Intervall atempting to send mails / do a respawn [in 100 ms] -> eg. 100 * 100ms = 10 sec
            maxSavedMails = 2000;                                    // How many mails you want to safe
            maxSavedMails_Player = 3;                                // How many mails can be sent per player
            daysTosaveMsg = 30;                                      // After how many days the message should get deleted
            debug_msg = true;                                        // Disable debug Messages for a cleaner console

            timevar_100ms = 0;
            lastReset = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// The Way every mail is safed.
    /// </summary>
    [Serializable]
    class Message
    {
        string sender;
        string destination;
        string content;
        DateTime timestamp;
        bool delivered;

        public Message(string sender_1, string destination_1, string content_1)
        {
            sender = sender_1;
            destination = destination_1;
            content = content_1;
            timestamp = DateTime.UtcNow;
            delivered = false;
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
                PerformInternalCommand("/respawn");

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



            if (IsPrivateMessage(text, ref message, ref username) && username.ToLower() != options.botname)
            {
                message = message.ToLower();
                cmd_reader(message, username, isMessageFromMod(username));
            }

        }

        /// <summary>
        /// Interprets command.
        /// </summary>
        public void cmd_reader(string message, string sender, bool isMod)
        {
            if (message.Contains("sendmail"))
            {
                GetMailsFromFile();
                if (getSentMessagesByUser(sender) < options.maxSavedMails_Player && logged_msg.Length < options.maxSavedMails)
                {


                    string content = "";
                    string destination = "";
                    bool destination_ended = false;

                    for (int i = message.IndexOf("sendmail") + 9; i < message.Length; i++) // + 4 -> get first letter of the name.
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
                        AddMail(sender, destination, content);
                    }
                    else
                    {
                        SendPrivateMessage(sender, "Something went wrong!");
                    }
                }
                else
                {
                    SendPrivateMessage(sender, "Couldn't save Message. Limit reached!");
                }
                clearLogged_msg();
            }

            if (isMod || options.moderator.Length == 0) // Delete the safe file of the bot to reset all mods || 2. otion to get the owner as a moderator.
            {
                mod_Commands(message, sender);
            }
        }

        private void mod_Commands(string message, string sender)
        {


            // These commands can be exploited easily and may cause huge damage.


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

            /*
             // Only uncomment for testing reasons 

            if (message.Contains("clearmails"))
            {
                if (debug_msg)
                {
                    LogToConsole("Clearing Messages.");
                }
                clearSavedMails();
            }
            */



            if (message.Contains("respawn"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Respawning! \n Performed by: " + sender);
                }
                PerformInternalCommand("/respawn");
            }

            if (message.Contains("deliver"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Sending Mails! \n Performed by: " + sender);
                }
                DeliverMail();
            }

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

            if (message.Contains("deleteoldmails"))
            {
                if (options.debug_msg)
                {
                    LogToConsole("Deleting old mails! \n Performed by: " + sender);
                }
                deleteOldMails();
                SaveMailsToFile();
            }

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

            if (message.Contains("daystosavemsg"))
            {
                options.daysTosaveMsg = getIntInCommand(message, "daystosavemsg");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed daystosavemsg to " + Convert.ToString(options.daysTosaveMsg));
                }
            }

            if (message.Contains("intervalsendmail"))
            {
                options.interval_sendmail = getIntInCommand(message, "intervalsendmail");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed intervalsendmail to " + Convert.ToString(options.interval_sendmail));
                }
            }

            if (message.Contains("maxsavedmails"))
            {
                options.maxSavedMails = getIntInCommand(message, "maxsavedmails");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed maxsavedmails to " + Convert.ToString(options.maxSavedMails));
                }
            }

            if (message.Contains("maxmailsperplayer"))
            {
                options.maxSavedMails_Player = getIntInCommand(message, "maxmailsperplayer");
                SaveOptionsToFile();
                SendPrivateMessage(sender, "Settings changed!");

                if (options.debug_msg)
                {
                    LogToConsole(sender + " changed maxmailsperplayer to " + Convert.ToString(options.maxSavedMails_Player));
                }
            }

            if (message.Contains("listsettings"))
            {
                SendPrivateMessage(sender, "debugmsg: " + Convert.ToString(options.debug_msg) + "; daystosavemsg: " + Convert.ToString(options.daysTosaveMsg) + "; intervalsendmail: " + Convert.ToString(options.interval_sendmail) + "; maxsavedmails: " + Convert.ToString(options.maxSavedMails) + "; maxsavedmails_player: " + Convert.ToString(options.maxSavedMails_Player));
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
                LogToConsole("Saved mails to File! \n" + "Location: " + options.path_mail + "\n Time: " + Convert.ToString(DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Get mails from save file.
        /// </summary>
        public void GetMailsFromFile()
        {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(options.path_mail, FileMode.Open, FileAccess.Read);

            logged_msg = (Message[])formatter.Deserialize(stream);
            stream.Close();

            if (options.debug_msg)
            {
                LogToConsole("Loaded mails from File! \n" + "Location: " + options.path_mail + "\n Time: " + Convert.ToString(DateTime.UtcNow));
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
                LogToConsole("Saved options to File! \n" + "Location: " + options.path_setting + "\n Time: " + Convert.ToString(DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Get settings from save file.
        /// </summary>
        public void GetOptionsFromFile()
        {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(options.path_setting, FileMode.Open, FileAccess.Read);

            options = (Options)formatter.Deserialize(stream);
            stream.Close();

            if (options.debug_msg)
            {
                LogToConsole("Loaded options from File! \n" + "Location: " + options.path_setting + "\n Time: " + Convert.ToString(DateTime.UtcNow));
            }
        }

        /// <summary>
        /// Add a message to the list.
        /// </summary>
        public void AddMail(string sender, string destination, string content)
        {
            GetMailsFromFile();

            Message[] tmp = logged_msg;
            logged_msg = new Message[logged_msg.Length + 1];

            for (int i = 0; i < tmp.Length; i++)
            {
                logged_msg[i] = tmp[i];
            }

            logged_msg[logged_msg.Length - 1] = new Message(sender, destination, content);

            SaveMailsToFile();
            SendPrivateMessage(sender, "Message saved!");
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
                        SendPrivateMessage(msg.GetDestination(), msg.GetSender() + " mailed: " + msg.GetContent());
                        msg.setDelivered();

                        if (options.debug_msg)
                        {
                            LogToConsole("Message of " + msg.GetSender() + " delivered to " + msg.GetDestination() + ".");
                        }
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
            int mailcount = 0;

            foreach (Message msg in logged_msg)
            {
                if (msg.GetSender() == player)
                {
                    mailcount++;
                }
            }
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
