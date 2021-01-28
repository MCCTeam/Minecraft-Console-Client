using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient
{
    public interface ILogger
    {
        bool DebugEnabled { get; set; }
        bool WarnEnabled { get; set; }
        bool InfoEnabled { get; set; }
        bool ErrorEnabled { get; set; }
        bool ChatEnabled { get; set; }

        void Info(string msg);
        void Info(string msg, params object[] args);
        void Info(object msg);

        void Debug(string msg);
        void Debug(string msg, params object[] args);
        void Debug(object msg);

        void Warn(string msg);
        void Warn(string msg, params object[] args);
        void Warn(object msg);

        void Error(string msg);
        void Error(string msg, params object[] args);
        void Error(object msg);

        void Chat(string msg);
        void Chat(string msg, params object[] args);
        void Chat(object msg);
    }
}
