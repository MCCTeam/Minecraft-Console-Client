using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Logger
{
    /// <summary>
    /// Abstract class providing basic implementation of the ILogger interface
    /// </summary>
    public abstract class LoggerBase : ILogger
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

        public abstract void Chat(string msg);

        public void Chat(string msg, params object[] args)
        {
            Chat(string.Format(msg, args));
        }

        public void Chat(object msg)
        {
            Chat(msg.ToString());
        }

        public abstract void Debug(string msg);

        public void Debug(string msg, params object[] args)
        {
            Debug(string.Format(msg, args));
        }

        public void Debug(object msg)
        {
            Debug(msg.ToString());
        }

        public abstract void Error(string msg);

        public void Error(string msg, params object[] args)
        {
            Error(string.Format(msg, args));
        }

        public void Error(object msg)
        {
            Error(msg.ToString());
        }

        public abstract void Info(string msg);

        public void Info(string msg, params object[] args)
        {
            Info(string.Format(msg, args));
        }

        public void Info(object msg)
        {
            Info(msg.ToString());
        }

        public abstract void Warn(string msg);

        public void Warn(string msg, params object[] args)
        {
            Warn(string.Format(msg, args));
        }

        public void Warn(object msg)
        {
            Warn(msg.ToString());
        }

        protected virtual void Log(object msg)
        {
            ConsoleIO.WriteLineFormatted(msg.ToString());
        }

        protected virtual void Log(string msg)
        {
            ConsoleIO.WriteLineFormatted(msg);
        }

        protected virtual void Log(string msg, params object[] args)
        {
            ConsoleIO.WriteLineFormatted(string.Format(msg, args));
        }
    }
}
