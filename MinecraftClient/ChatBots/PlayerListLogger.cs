using System;
using System.Text;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot sends a /list command every X seconds and save the result.
    /// </summary>

    public class PlayerListLogger : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "PlayerListLogger";

            public bool Enabled = false;

            public string File = "playerlog.txt";

            [TomlInlineComment("$config.ChatBot.PlayerListLogger.Delay$")]
            public int Delay = 600;

            public void OnSettingUpdate()
            {
                File ??= string.Empty;

                if (Delay < 10)
                    Delay = 10;
            }
        }

        private int count = 0;

        public override void Update()
        {
            count++;
            if (count == Config.Delay)
            {
                DateTime now = DateTime.Now;

                LogDebugToConsole("Saving Player List");

                StringBuilder sb = new();
                sb.AppendLine(string.Format("[{0}/{1}/{2} {3}:{4}]", now.Year, now.Month, now.Day, now.Hour, now.Minute));
                sb.AppendLine(string.Join(", ", GetOnlinePlayers())).AppendLine();
                System.IO.File.AppendAllText(Settings.Config.AppVar.ExpandVars(Config.File), sb.ToString());

                count = 0;
            }
        }
    }
}
