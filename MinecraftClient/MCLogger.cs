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
        public bool DebugEnabled { get { return debugEnabled; } set { debugEnabled = value; } }
        public bool WarnEnabled { get { return warnEnabled; } set { warnEnabled = value; } }
        public bool InfoEnabled { get { return infoEnabled; } set { infoEnabled = value; } }
        public bool ErrorEnabled { get { return errorEnabled; } set { errorEnabled = value; } }
        public bool ChatEnabled { get { return chatEnabled; } set { chatEnabled = value; } }

        public void Debug(string msg)
        {
            if (debugEnabled)
            {
                if (Settings.DebugFilter != null)
                {
                    var shouldLog = Settings.DebugFilter.IsMatch(msg); // assumed whitelist mode
                    if (Settings.FilterMode == Settings.FilterModeEnum.Blacklist)
                        shouldLog = !shouldLog; // blacklist mode so flip result
                    if (!shouldLog)
                        return;
                    // Don't write debug lines here as it could cause a stack overflow
                }
                Log("§8[DEBUG] " + msg);
            }
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
                if (Settings.ChatFilter != null)
                {
                    var shouldLog = Settings.ChatFilter.IsMatch(msg); // assumed whitelist mode
                    if (Settings.FilterMode == Settings.FilterModeEnum.Blacklist)
                        shouldLog = !shouldLog; // blacklist mode so flip result
                    if (shouldLog)
                        Log(msg);
                    else Debug("[Logger] One Chat message filtered: " + msg);
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
