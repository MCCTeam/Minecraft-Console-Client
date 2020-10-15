using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Record and save replay file that can be used by the Replay mod (https://www.replaymod.com/)
    /// </summary>
    public class ReplayCapture : ChatBot
    {
        private ReplayHandler replay;
        private int backupInterval = 3000; // Unit: second * 10
        private int backupCounter = -1;

        public ReplayCapture(int backupInterval)
        {
            if (backupInterval != -1)
                this.backupInterval = backupInterval * 10;
            else this.backupInterval = -1;
        }

        public override void Initialize()
        {
            SetNetworkPacketEventEnabled(true);
            replay = new ReplayHandler(GetProtocolVersion());
            replay.MetaData.serverName = GetServerHost() + GetServerPort();
            backupCounter = backupInterval;

            RegisterChatBotCommand("replay", Translations.Get("bot.replayCapture.cmd"), "replay <save|stop>", Command);
        }

        public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
        {
            replay.AddPacket(packetID, packetData, isLogin, isInbound);
        }

        public override void Update()
        {
            if (backupInterval > 0 && replay.RecordRunning)
            {
                if (backupCounter <= 0)
                {
                    replay.CreateBackupReplay(@"recording_cache\REPLAY_BACKUP.mcpr");
                    backupCounter = backupInterval;
                }
                else backupCounter--;
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            replay.OnShutDown();
            return base.OnDisconnect(reason, message);
        }

        public string Command(string cmd, string[] args)
        {
            try
            {
                if (replay.RecordRunning)
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
