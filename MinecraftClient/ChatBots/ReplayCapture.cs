using System;
using System.Collections.Generic;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Protocol;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Record and save replay file that can be used by the Replay mod (https://www.replaymod.com/)
    /// </summary>
    public class ReplayCapture : ChatBot
    {
        public const string CommandName = "replay";

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "ReplayCapture";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.ReplayCapture.Backup_Interval$")]
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

            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("save")
                    .Executes(r => OnCommandSave(r.Source)))
                .Then(l => l.Literal("stop")
                    .Executes(r => OnCommandStop(r.Source)))
                .Then(l => l.Literal("_help")
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );
        }

        public override void OnUnload()
        {
            McClient.dispatcher.Unregister(CommandName);
            McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
        }

        private int OnCommandHelp(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>   string.Format(Translations.general_available_cmd, "save, stop")
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandSave(CmdResult r)
        {
            try
            {
                if (replay!.RecordRunning)
                {
                    replay.CreateBackupReplay(@"replay_recordings\" + replay.GetReplayDefaultName());
                    return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_replayCapture_created);
                }
                else
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_replayCapture_restart);
            }
            catch (Exception e)
            {
                return r.SetAndReturn(CmdResult.Status.Fail, e.Message);
            }
        }

        private int OnCommandStop(CmdResult r)
        {
            try
            {
                if (replay!.RecordRunning)
                {
                    replay.OnShutDown();
                    return r.SetAndReturn(CmdResult.Status.Done, Translations.bot_replayCapture_stopped);
                }
                else
                    return r.SetAndReturn(CmdResult.Status.Fail, Translations.bot_replayCapture_restart);
            }
            catch (Exception e)
            {
                return r.SetAndReturn(CmdResult.Status.Fail, e.Message);
            }
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
    }
}
