using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MinecraftClient.Logger
{
    public class FileLogLogger : FilteredLogger
    {
        private string logFile;
        private bool prependTimestamp;
        private object logFileLock = new object();

        public FileLogLogger(string file, bool prependTimestamp = false)
        {
            logFile = file;
            this.prependTimestamp = prependTimestamp;
            Save("### Log started at " + GetTimestamp() + " ###");
        }

        private void LogAndSave(string msg)
        {
            Log(msg);
            Save(msg);
        }

        private void Save(string msg)
        {
            try
            {
                if (!Settings.SaveColorCodes)
                    msg = ChatBot.GetVerbatim(msg);
                if (prependTimestamp)
                    msg = GetTimestamp() + ' ' + msg;

                string directory = Path.GetDirectoryName(logFile);
                if (!String.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                lock (logFileLock)
                {
                    FileStream stream = new FileStream(logFile, FileMode.OpenOrCreate);
                    StreamWriter writer = new StreamWriter(stream);
                    stream.Seek(0, SeekOrigin.End);
                    writer.WriteLine(msg);
                    writer.Dispose();
                    stream.Close();
                }
            }
            catch (Exception e)
            {
                // Must use base since we already failed to write log
                base.Error("Cannot write to log file: " + e.Message);
                base.Debug("Stack trace: \n" + e.StackTrace);
            }
        }

        private static string GetTimestamp()
        {
            DateTime time = DateTime.Now;
            return String.Format("{0}-{1}-{2} {3}:{4}:{5}",
                time.Year.ToString("0000"),
                time.Month.ToString("00"),
                time.Day.ToString("00"),
                time.Hour.ToString("00"),
                time.Minute.ToString("00"),
                time.Second.ToString("00"));
        }

        public override void Chat(string msg)
        {
            if (ChatEnabled)
            {
                if (ShouldDisplay(FilterChannel.Chat, msg))
                {
                    LogAndSave(msg);
                }
                else Debug("[Logger] One Chat message filtered: " + msg);
            }
        }

        public override void Debug(string msg)
        {
            if (DebugEnabled)
            {
                if (ShouldDisplay(FilterChannel.Debug, msg))
                {
                    LogAndSave("§8[DEBUG] " + msg);
                }
            }
        }

        public override void Error(string msg)
        {
            base.Error(msg);
            if (ErrorEnabled)
                Save(msg);
        }

        public override void Info(string msg)
        {
            base.Info(msg);
            if (InfoEnabled)
                Save(msg);
        }

        public override void Warn(string msg)
        {
            base.Warn(msg);
            if (WarnEnabled)
                Save(msg);
        }
    }
}
