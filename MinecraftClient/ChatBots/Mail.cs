using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Collections.Generic;

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
        public int timevar_100ms { get; set; }
        public Dictionary<string, bool> bools { get; set; }
        public Dictionary<string, int> integer { get; set; }
        public string[] ignored = new string[0];
        public DateTime lastReset { get; set; }


        public Options()
        {
            path_mail = AppDomain.CurrentDomain.BaseDirectory + "MailDatabase.ini";            // Path where the mail file is saved. You can also apply a normal path like @"C:\Users\SampleUser\Desktop"
            path_setting = AppDomain.CurrentDomain.BaseDirectory + "MailBotSettings.ini";       // Path where the settings are saved

            integer = new Dictionary<string, int>();
            integer.Add("interval_sendmail", 100);                                      // Intervall atempting to send mails / do a respawn [in 100 ms] -> eg. 100 * 100ms = 10 sec
            integer.Add("maxsavedmails", 2000);                                         // How many mails you want to safe
            integer.Add("maxsavedmails_player", 3);                                     // How many mails can be sent per player
            integer.Add("daystosavemsg", 30);                                           // After how many days the message should get deleted
            integer.Add("maxcharsinmsg", 255);                                          // How many characters can be in a message (Only content, without command syntax)

            bools = new Dictionary<string, bool>();
            bools.Add("auto_respawn", true);                                            // Toggle the internal autorespawn
            bools.Add("allow_sendmail", true);                                         // Enable the continious mail sending
            bools.Add("allow_receivemail", true);                                      // Enable the bot reacting to command
            bools.Add("allow_selfmail", true);                                          // Enable to send mails to yourself (mainly for test reason)
            bools.Add("allow_publiccommands", false);                                   // Should the bot accept commands from normal chat?
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

        public Message(string sender_1, string destination_1, string content_1, bool anonymous_1, DateTime timestamp1)
        {
            sender = sender_1;
            destination = destination_1;
            content = content_1;
            timestamp = timestamp1;
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

            RegisterChatBotCommand("mail", "Options: addignored; changemailpath; changesettingspath; getignored; getmails; removeignored; resettimer; updatemails; setbool; setinteger;", internalCommandInterpreter);

        }

        /// <summary>
        /// Standard settings for the bot.
        /// </summary>
        public override void AfterGameJoined()
        {
            LogToConsole("Join time: " + DateTime.UtcNow + " UTC.");

            if (!File.Exists(options.path_setting))
            {
                serializeOptions();
            }
            else
            {
                deserializeOptions();
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
            if (options.timevar_100ms == options.integer["interval_sendmail"])
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

                if (IsPrivateMessage(text, ref message, ref username) || (IsChatMessage(text, ref message, ref username) && options.bools["allow_publiccommands"]))
                {
                    if (!isIgnored(username))
                    {
                        Message[] msg_array = getMailsFromFile();

                        if (username.ToLower() != options.botname.ToLower() && getSentMessagesByUser(username) < options.integer["maxsavedmails_player"] && msg_array.Length < options.integer["maxsavedmails"])
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
            else if (options.bools["debug_msg"])
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

                    if (temp_content.Length <= options.integer["maxcharsinmsg"]) // Is the content length within the given range of the host
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
                SendPrivateMessage(sender, "Something went wrong! Max characters: " + options.integer["maxcharsinmsg"]);
            }
        }

        /// <summary>
        /// Interprets local commands.
        /// </summary>
        public string internalCommandInterpreter(string cmd, string[] args)
        {
            switch (args[0])
            {
                case "addignored":
                    return addIgnored(cmd, args);

                case "changemailpath":
                    return changeMailPath(cmd, args);

                case "changesettingspath":
                    return changeSettingsPath(cmd, args);

                case "getignored":
                    return getIgnored();

                case "getmails":
                    return getMails();

                case "getsettings":
                    return getSettings();

                case "removeignored":
                    return removeIgnored(cmd, args);

                case "resettimer":
                    return resetTimer();

                case "updatemails":
                    return updateMails();

                case "setbool":
                    return setBooleans(cmd, args);

                case "setinteger":
                    return setInteger(cmd, args);
            }
            return "No option found! Do '/help mail'!";
        }

        /// <summary>
        /// Toggles all bools.
        /// Syntax args[0]="setbool"; args[1]="boolname"; args[2]="value";
        /// </summary>
        public string setBooleans(string cmd, string[] args)
        {
            if (args.Length > 1) // toggle is allowed!
            {
                if (args[1] == "help")
                {
                    return "Options: " + string.Join("; ", options.bools.Keys.ToArray());
                }
                else
                {
                    try
                    {
                        if (args.Length > 2)                                            // Check if a vlaue is given
                        { options.bools[args[1]] = bool.Parse(args[2].ToLower()); }     // If yes, try to set the bool to this value
                        else { options.bools[args[1]] = !options.bools[args[1]]; }      // Otherwise toggle.

                        serializeOptions();
                        return "Changed " + args[1] + " to: " + (options.bools[args[1]]).ToString();
                    }
                    catch (Exception)
                    {
                        return "Missing or wrong argument! Add 'help' to get all available options!";
                    }
                }
            }
            else
            {
                return "Missing or wrong argument! Add 'help' to get all available options!";
            }
        }

        /// <summary>
        /// Change Integers.
        /// </summary>
        public string setInteger(string cmd, string[] args)
        {
            if (args.Length > 2) // You MUST enter a value!
            {
                if (args[1] == "help")
                {
                    return "Options: " + string.Join("; ", options.integer.Keys.ToArray());
                }
                else
                {
                    try
                    {
                        options.integer[args[1]] = Int32.Parse(args[2]);
                        serializeOptions();
                        return "Changed " + args[1] + " to: " + (options.integer[args[1]]).ToString();
                    }
                    catch (Exception)
                    {
                        return "Missing or wrong argument! Add 'help' to get all available options!";
                    }
                }
            }
            else
            {
                return "Missing or wrong argument! Add 'help' to get all available options!";
            }
        }

        /// <summary>
        /// Change the file path of mails.
        /// </summary>
        public string changeMailPath(string cmd, string[] args)
        {
            if (args.Length > 1)
            {
                options.path_mail = AppDomain.CurrentDomain.BaseDirectory + args[1];
                serializeOptions();
                deserializeOptions();

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
            if (args.Length > 1)
            {
                options.path_setting = AppDomain.CurrentDomain.BaseDirectory + args[1];
                serializeOptions();
                deserializeOptions();

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
        public string resetTimer()
        {
            options.timevar_100ms = 0;

            return "Resetted Timer. At" + DateTime.UtcNow + " UTC";
        }

        /// <summary>
        /// List all settings.
        /// </summary>
        public string getSettings()
        {
            return "\n allow_publiccommands: "
                + (options.bools["allow_publiccommands"]).ToString()
                + ";\n allow_receivemail: "
                + (options.bools["allow_receivemail"]).ToString()
                + ";\n auto_respawn: "
                + (options.bools["auto_respawn"]).ToString()
                + ";\n allow_selfmail: "
                + (options.bools["allow_selfmail"]).ToString()
                + ";\n allow_sendmail: "
                + (options.bools["allow_sendmail"]).ToString()
                + ";\n daystosavemsg: "
                + (options.integer["daystosavemsg"]).ToString()
                + "\n debug_msg: "
                + (options.bools["debug_msg"]).ToString()
                + ";\n intervalsendmail: "
                + (options.integer["interval_sendmail"]).ToString()
                + ";\n maxcharsinmail: "
                + (options.integer["maxcharsinmsg"]).ToString()
                + ";\n maxsavedmails: "
                + (options.integer["maxsavedmails"]).ToString()
                + ";\n maxsavedmails_player: "
                + (options.integer["maxsavedmails_player"]).ToString()
                + ";\n messagepath: "
                + options.path_mail
                + ";\n settingspath: "
                + options.path_setting;
        }

        /// <summary>
        /// Get all safed mails.
        /// </summary>
        public string getMails()
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
        public string updateMails()
        {
            update_and_send_mails();
            return "Sending / Deleting mails!";
        }

        /// <summary>
        /// Add an ignored player to the list.
        /// </summary>
        public string addIgnored(string cmd, string[] args)
        {
            if (args.Length > 1 && IsValidName(args[1]))
            {
                options.ignored = addMember(args[1], options.ignored);
                serializeOptions();

                return "Added " + args[1] + " as ignored!";
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
            if (args.Length > 1 && IsValidName(args[1]))
            {
                options.ignored = removeMember(args[1], options.ignored);
                serializeOptions();

                return "Removed " + args[1] + " as ignored!";
            }
            else
            {
                return "Your name shall not pass!";
            }
        }

        /// <summary>
        /// List ignored players.
        /// </summary>
        public string getIgnored()
        {
            LogToConsole("Ignored are:");

            foreach (string name in options.ignored)
            {
                LogToConsole(name);
            }

            return "";
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
                serializeMail(msg_array);

                if (options.bools["debug_msg"])
                {
                    LogToConsole("Saved mails to File!" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                }
            }
            catch (Exception) // If, by any reason, the file couldn't be safed:
            {
                
                options.path_mail = AppDomain.CurrentDomain.BaseDirectory + "MailDatabase.ini";
                serializeOptions();

                LogToConsole("Directory or File not Found! Path changed to:" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");

                serializeMail(msg_array);

                if (options.bools["debug_msg"])
                {
                    LogToConsole("Saved mails to File!" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                }
            }
        }

        /// <summary>
        /// Get mails from save file.
        /// </summary>
        public Message[] getMailsFromFile()
        {
            // Tries to access file and creates a new one, if path doesn't exist, to avoid issues.

            try
            {
                if (options.bools["debug_msg"])
                {
                    LogToConsole("Loaded mails from File!" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                }

                return deserializeMail();
            }
            catch (Exception)
            {
                options.path_mail = AppDomain.CurrentDomain.BaseDirectory + "MailDatabase.ini";
                serializeOptions();

                LogToConsole("Directory or File not Found! Path changed to:" + " Location: " + options.path_mail + " Time: " + DateTime.UtcNow + " UTC");
                return logged_msg;
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

            msg_array[msg_array.Length - 1] = new Message(sender, destination, content, anonymous, DateTime.UtcNow);

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
            foreach (string Player in GetOnlinePlayers())
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
            for (int i = 0; i < msg_array.Length; i++)
            {
                if ((DateTime.UtcNow - msg_array[i].GetTimeStamp()).Days > options.integer["daystosavemsg"])
                {
                    msg_array[i].setDelivered();
                }
            }
            msg_array = msg_array.Where(x => !x.isDelivered()).ToArray();

            return msg_array;
        }

        /// <summary>
        /// Serialize the option class.
        /// </summary>
        public void serializeOptions()
        {
            Dictionary<string, Dictionary<string, string>> iniFileDict = new Dictionary<string, Dictionary<string, string>>();
            
            Dictionary<string, string> iniSection = new Dictionary<string, string>();
            iniFileDict["Settings"] = iniSection;
            iniSection["path_mail"] = options.path_mail;
            iniSection["path_setting"] = options.path_setting;

            iniSection["interval_sendmail"] = options.integer["interval_sendmail"].ToString();
            iniSection["maxsavedmails"] = options.integer["maxsavedmails"].ToString();
            iniSection["maxsavedmails_player"] = options.integer["maxsavedmails_player"].ToString();
            iniSection["daystosavemsg"] = options.integer["daystosavemsg"].ToString();
            iniSection["maxcharsinmsg"] = options.integer["maxcharsinmsg"].ToString();

            iniSection["auto_respawn"] = options.bools["auto_respawn"].ToString();
            iniSection["allow_sendmail"] = options.bools["allow_sendmail"].ToString();
            iniSection["allow_receivemail"] = options.bools["allow_receivemail"].ToString();
            iniSection["allow_selfmail"] = options.bools["allow_selfmail"].ToString();
            iniSection["allow_publiccommands"] = options.bools["allow_publiccommands"].ToString();
            iniSection["debug_msg"] = options.bools["debug_msg"].ToString();

            INIFile.WriteFile(options.path_setting, iniFileDict, "MailBot Settings");

            if(options.bools["debug_msg"])
            {
                LogToConsole("Saved options to File! " + "Location: " + options.path_setting + " Time: " + DateTime.UtcNow + " UTC");
            }
        }

        /// <summary>
        /// Deserialize the option class.
        /// </summary>
        public void deserializeOptions()
        {
            Dictionary<string, Dictionary<string, string>> iniFileDict = INIFile.ParseFile(options.path_setting);
            
            foreach (KeyValuePair<string, Dictionary<string, string>> iniSection in iniFileDict)
            {
                options.path_mail = iniSection.Value["path_mail"];
                options.path_setting = iniSection.Value["path_setting"];

                options.integer["interval_sendmail"] = Int32.Parse(iniSection.Value["interval_sendmail"]);
                options.integer["maxsavedmails"] = Int32.Parse(iniSection.Value["maxsavedmails"]);
                options.integer["maxsavedmails_player"] = Int32.Parse(iniSection.Value["maxsavedmails_player"]);
                options.integer["daystosavemsg"] = Int32.Parse(iniSection.Value["daystosavemsg"]);
                options.integer["maxcharsinmsg"] = Int32.Parse(iniSection.Value["maxcharsinmsg"]);

                options.bools["auto_respawn"] = bool.Parse(iniSection.Value["auto_respawn"]);
                options.bools["allow_sendmail"] = bool.Parse(iniSection.Value["allow_sendmail"]);
                options.bools["allow_receivemail"] = bool.Parse(iniSection.Value["allow_receivemail"]);
                options.bools["allow_selfmail"] = bool.Parse(iniSection.Value["allow_selfmail"]);
                options.bools["allow_publiccommands"] = bool.Parse(iniSection.Value["allow_publiccommands"]);
                options.bools["debug_msg"] = bool.Parse(iniSection.Value["debug_msg"]);
            }

            if (options.bools["debug_msg"])
            {
                LogToConsole("Loaded options from File! " + "Location: " + options.path_setting + " Time: " + DateTime.UtcNow + " UTC");
            }
        }

        /// <summary>
        /// Serialize the mail class.
        /// </summary>
        public void serializeMail(Message[] msgList)
        {
            Dictionary<string, Dictionary<string, string>> iniFileDict = new Dictionary<string, Dictionary<string, string>>();
            for (int msgNum = 0; msgNum < msgList.Length; msgNum++)
            {
                Dictionary<string, string> iniSection = new Dictionary<string, string>();
                Message msg = msgList[msgNum];
                iniSection["sender"] = msg.GetSender();
                iniSection["destination"] = msg.GetDestination();
                iniSection["content"] = msg.GetContent();
                iniSection["timestamp"] = msg.GetTimeStamp().ToString();
                iniSection["anonymous"] = msg.isAnonymous().ToString();
                iniFileDict["mail" + msgNum] = iniSection;
            }
            INIFile.WriteFile(options.path_mail, iniFileDict, "Mail Database");
            
        }

        /// <summary>
        /// Deserialize the mail class.
        /// </summary>
        public Message[] deserializeMail()
        {
            Dictionary<string, Dictionary<string, string>> iniFileDict = INIFile.ParseFile(options.path_mail);
            List<Message> messages = new List<Message>();
            foreach (KeyValuePair<string, Dictionary<string, string>> iniSection in iniFileDict)
            {
                //iniSection.Key is "mailXX" but we don't need it here
                string sender = iniSection.Value["sender"];
                string destination = iniSection.Value["destination"];
                string content = iniSection.Value["content"];
                DateTime timestamp = DateTime.Parse(iniSection.Value["timestamp"]);
                bool anonymous = INIFile.Str2Bool(iniSection.Value["anonymous"]);
                messages.Add(new Message(sender, destination, content, anonymous, timestamp)); //TODO timestamp
            }
            return messages.ToArray();
        }
    }
}
