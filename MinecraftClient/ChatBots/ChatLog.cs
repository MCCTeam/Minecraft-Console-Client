using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MinecraftClient.ChatBots
{
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
            if (String.IsNullOrEmpty(file) || file.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                LogToConsole("Path '" + file + "' contains invalid characters.");
                UnloadBot();
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
            text = GetVerbatim(text);
            string sender = "";
            string message = "";

            if (saveChat && IsChatMessage(text, ref message, ref sender))
            {
                save("Chat " + sender + ": " + message);
            }
            else if (savePrivate && IsPrivateMessage(text, ref message, ref sender))
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
                tosave = GetTimestamp() + ' ' + tosave;

            string directory = Path.GetDirectoryName(logfile);
            if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            FileStream stream = new FileStream(logfile, FileMode.OpenOrCreate);
            StreamWriter writer = new StreamWriter(stream);
            stream.Seek(0, SeekOrigin.End);
            writer.WriteLine(tosave);
            writer.Dispose();
            stream.Close();
        }
    }
}
