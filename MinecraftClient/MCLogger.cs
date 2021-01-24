using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient
{
    public class MCLogger : ILogger
    {
        private bool debugEnabled = false;
        private bool warnEnabled = true;
        private bool infoEnabled = true;
        private bool errorEnabled = true;
        private bool chatEnabled = true;
        public bool DebugEnabled { get => debugEnabled; set => debugEnabled = value; }
        public bool WarnEnabled { get => warnEnabled; set => warnEnabled = value; }
        public bool InfoEnabled { get => infoEnabled; set => infoEnabled = value; }
        public bool ErrorEnabled { get => errorEnabled; set => errorEnabled = value; }
        public bool ChatEnabled { get => chatEnabled; set => chatEnabled = value; }

        public void Debug(string msg)
        {
            if (debugEnabled)
                Log("§8[DEBUG] " + msg);
        }

        public void Debug(string msg, params object[] args)
        {
            Debug(string.Format(msg, args));
        }

        public void Debug(object msg)
        {
            Debug(msg.ToString());
        }

        public void Info(object msg)
        {
            if (infoEnabled)
                ConsoleIO.WriteLogLine(msg.ToString());
        }

        public void Info(string msg)
        {
            if (infoEnabled)
                ConsoleIO.WriteLogLine(msg);
        }

        public void Info(string msg, params object[] args)
        {
            if (infoEnabled)
                ConsoleIO.WriteLogLine(string.Format(msg, args));
        }

        public void Warn(string msg)
        {
            if (warnEnabled)
                Log("§6[WARN] " + msg);
        }

        public void Warn(string msg, params object[] args)
        {
            Warn(string.Format(msg, args));
        }

        public void Warn(object msg)
        {
            Warn(msg.ToString());
        }

        public void Error(string msg)
        {
            if (errorEnabled)
                Log("§c[ERROR] " + msg);
        }

        public void Error(string msg, params object[] args)
        {
            Error(string.Format(msg, args));
        }

        public void Error(object msg)
        {
            Error(msg.ToString());
        }

        public void Chat(string msg)
        {
            if (chatEnabled)
            {
                if (Settings.ChatFilter != null && Settings.ChatFilter.IsMatch(msg))
                {
                    Debug("[Logger] One Chat message filtered");
                }
                else Log(msg);
            }
        }

        public void Chat(string msg, params object[] args)
        {
            Chat(string.Format(msg, args));
        }

        public void Chat(object msg)
        {
            Chat(msg.ToString());
        }

        private void Log(object msg)
        {
            ConsoleIO.WriteLineFormatted(msg.ToString());
        }

        private void Log(string msg)
        {
            ConsoleIO.WriteLineFormatted(msg);
        }

        private void Log(string msg, params object[] args)
        {
            ConsoleIO.WriteLineFormatted(string.Format(msg, args));
        }
    }
}
