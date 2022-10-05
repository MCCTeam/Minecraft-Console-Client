using System.Text.RegularExpressions;
using static MinecraftClient.Settings;

namespace MinecraftClient.Logger
{
    public class FilteredLogger : LoggerBase
    {
        protected enum FilterChannel { Debug, Chat }

        protected bool ShouldDisplay(FilterChannel channel, string msg)
        {
            Regex? regexToUse = null;
            // Convert to bool for XOR later. Whitelist = 0, Blacklist = 1
            bool filterMode = Config.Logging.FilterMode == LoggingConfigHealper.LoggingConfig.FilterModeEnum.blacklist;
            switch (channel)
            {
                case FilterChannel.Chat:
                    string chat = Config.Logging.ChatFilterRegex;
                    if (string.IsNullOrEmpty(chat))
                        regexToUse = null;
                    else
                        regexToUse = new(chat);
                    break;
                case FilterChannel.Debug:
                    string debug = Config.Logging.DebugFilterRegex;
                    if (string.IsNullOrEmpty(debug))
                        regexToUse = null;
                    else
                        regexToUse = new(debug);
                    break;
            }
            if (regexToUse != null)
            {
                // IsMatch and white/blacklist result can be represented using XOR
                // e.g.  matched(true) ^ blacklist(true) => shouldn't log(false)
                return regexToUse.IsMatch(msg) ^ filterMode;
            }
            else return true;
        }

        public override void Debug(string msg)
        {
            if (DebugEnabled)
            {
                if (ShouldDisplay(FilterChannel.Debug, msg))
                {
                    Log("§8[DEBUG] " + msg);
                }
                // Don't write debug lines here as it could cause a stack overflow
            }
        }

        public override void Info(string msg)
        {
            if (InfoEnabled)
                ConsoleIO.WriteLogLine(msg);
        }

        public override void Warn(string msg)
        {
            if (WarnEnabled)
                Log("§6[WARN] " + msg);
        }

        public override void Error(string msg)
        {
            if (ErrorEnabled)
                Log("§c[ERROR] " + msg);
        }

        public override void Chat(string msg)
        {
            if (ChatEnabled)
            {
                if (ShouldDisplay(FilterChannel.Chat, msg))
                {
                    Log(msg);
                }
                else Debug("[Logger] One Chat message filtered: " + msg);
            }
        }
    }
}
