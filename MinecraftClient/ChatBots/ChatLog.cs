using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
}
