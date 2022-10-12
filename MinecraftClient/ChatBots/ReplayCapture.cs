using System;
using System.Collections.Generic;
using MinecraftClient.Protocol;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Record and save replay file that can be used by the Replay mod (https://www.replaymod.com/)
    /// </summary>
    public class ReplayCapture : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "ReplayCapture";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.ReplayCapture.Backup_Interval$")]
            public double Backup_Interval = 300.0;

            public void OnSettingUpdate()
            {
                if (Backup_Interval < -1)
                    Backup_Interval = -1;
            }
        }

        private ReplayHandler? replay;
        private int backupCounter = -1;

        public override void Initialize()
        {
            SetNetworkPacketEventEnabled(true);
            replay = new ReplayHandler(GetProtocolVersion());
            replay.MetaData.serverName = GetServerHost() + GetServerPort();
            backupCounter = Settings.DoubleToTick(Config.Backup_Interval);

            RegisterChatBotCommand("replay", Translations.Get("bot.replayCapture.cmd"), "replay <save|stop>", Command);
        }

        public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
        {
            replay!.AddPacket(packetID, packetData, isLogin, isInbound);
        }

        public override void Update()
        {
            if (Config.Backup_Interval > 0 && replay!.RecordRunning)
            {
                if (backupCounter <= 0)
                {
                    replay.CreateBackupReplay(@"recording_cache\REPLAY_BACKUP.mcpr");
                    backupCounter = Settings.DoubleToTick(Config.Backup_Interval);
                }
                else backupCounter--;
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            replay!.OnShutDown();
            return base.OnDisconnect(reason, message);
        }

        public string Command(string cmd, string[] args)
        {
            try
            {
                if (replay!.RecordRunning)
                {
                    if (args.Length > 0)
                    {
                        switch (args[0].ToLower())
                        {
                            case "save":
                                {
                                    replay.CreateBackupReplay(@"replay_recordings\" + replay.GetReplayDefaultName());
                                    return Translations.Get("bot.replayCapture.created");
                                }
                            case "stop":
                                {
                                    replay.OnShutDown();
                                    return Translations.Get("bot.replayCapture.stopped");
                                }
                        }
                    }
                    return Translations.Get("general.available_cmd", "save, stop");
                }
                else return Translations.Get("bot.replayCapture.restart");
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
