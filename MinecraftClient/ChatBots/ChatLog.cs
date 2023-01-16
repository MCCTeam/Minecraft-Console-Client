using System;
using System.IO;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot saves the received messages in a text file.
    /// </summary>

    public class ChatLog : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "ChatLog";

            public bool Enabled = false;

            public bool Add_DateTime = true;

            public string Log_File = @"chatlog-%username%-%serverip%.txt";

            public MessageFilter Filter = MessageFilter.messages;

            public void OnSettingUpdate()
            {
                Log_File ??= string.Empty;

                if (!Enabled) return;

                string Log_File_Full = Settings.Config.AppVar.ExpandVars(Log_File);
                if (String.IsNullOrEmpty(Log_File_Full) || Log_File_Full.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    LogToConsole(BotName, string.Format(Translations.bot_chatLog_invalid_file, Log_File_Full));
                    LogToConsole(BotName, Translations.general_bot_unload);
                    Enabled = false;
                }
            }

            public enum MessageFilter { all, messages, chat, private_chat, internal_msg };
        }

        private bool saveOther = true;
        private bool saveChat = true;
        private bool savePrivate = true;
        private bool saveInternal = true;
        private readonly object logfileLock = new();

        /// <summary>
        /// This bot saves the messages received in the specified file, with some filters and date/time tagging.
        /// </summary>
        /// <param name="file">The file to save the log in</param>
        /// <param name="filter">The kind of messages to save</param>
        /// <param name="AddDateAndTime">Add a date and time before each message</param>

        public ChatLog()
        {
            UpdateFilter(Config.Filter);
        }

        public void UpdateFilter(Configs.MessageFilter filter)
        {
            switch (filter)
            {
                case Configs.MessageFilter.all:
                    saveOther = true;
                    savePrivate = true;
                    saveChat = true;
                    break;
                case Configs.MessageFilter.messages:
                    saveOther = false;
                    savePrivate = true;
                    saveChat = true;
                    break;
                case Configs.MessageFilter.chat:
                    saveOther = false;
                    savePrivate = false;
                    saveChat = true;
                    break;
                case Configs.MessageFilter.private_chat:
                    saveOther = false;
                    savePrivate = true;
                    saveChat = false;
                    break;
                case Configs.MessageFilter.internal_msg:
                    saveOther = false;
                    savePrivate = false;
                    saveChat = false;
                    saveInternal = true;
                    break;
            }
        }

        public override void GetText(string text)
        {
            text = GetVerbatim(text);
            string sender = "";
            string message = "";

            if (saveChat && IsChatMessage(text, ref message, ref sender))
            {
                Save("Chat " + sender + ": " + message);
            }
            else if (savePrivate && IsPrivateMessage(text, ref message, ref sender))
            {
                Save("Private " + sender + ": " + message);
            }
            else if (saveOther)
            {
                Save("Other: " + text);
            }
        }

        public override void OnInternalCommand(string commandName, string commandParams, CmdResult result)
        {
            if (saveInternal)
            {
                Save(string.Format("Internal {0}({1}): {2}", commandName, commandParams, result));
            }
        }

        private void Save(string tosave)
        {
            if (Config.Add_DateTime)
                tosave = GetTimestamp() + ' ' + tosave;
            string Log_File_Full = Settings.Config.AppVar.ExpandVars(Config.Log_File);
            lock (logfileLock)
            {
                string? directory = Path.GetDirectoryName(Log_File_Full);
                if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                FileStream stream = new(Log_File_Full, FileMode.OpenOrCreate);
                StreamWriter writer = new(stream);
                stream.Seek(0, SeekOrigin.End);
                writer.WriteLine(tosave);
                writer.Dispose();
                stream.Close();
            }
        }
    }
}
