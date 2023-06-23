using System;
using System.Linq;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class FollowPlayer : ChatBot
    {
        public const string CommandName = "follow";

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized] private const string BotName = "FollowPlayer";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.FollowPlayer.Update_Limit$")]
            public double Update_Limit = 1.5;

            [TomlInlineComment("$ChatBot.FollowPlayer.Stop_At_Distance$")]
            public double Stop_At_Distance = 3.0;

            public void OnSettingUpdate()
            {
                if (Update_Limit < 0)
                    Update_Limit = 0;

                if (Stop_At_Distance < 0)
                    Stop_At_Distance = 0;
            }
        }

        private string? _playerToFollow = null;
        private int _updateCounter = 0;
        private bool _unsafeEnabled = false;

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsole(Translations.extra_entity_required);
                LogToConsole(Translations.general_bot_unload);
                UnloadBot();
                return;
            }

            if (!GetTerrainEnabled())
            {
                LogToConsole(Translations.extra_terrainandmovement_required);
                LogToConsole(Translations.general_bot_unload);
                UnloadBot();
                return;
            }

            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("start")
                    .Then(l => l.Argument("PlayerName", MccArguments.PlayerName())
                        .Executes(r => OnCommandStart(r.Source, Arguments.GetString(r, "PlayerName"), takeRisk: false))
                        .Then(l => l.Literal("-f")
                            .Executes(r =>
                                OnCommandStart(r.Source, Arguments.GetString(r, "PlayerName"), takeRisk: true)))))
                .Then(l => l.Literal("stop")
                    .Executes(r => OnCommandStop(r.Source)))
                .Then(l => l.Literal("_help")
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );
        }

        public override void OnUnload()
        {
            BotMovementLock.Instance?.UnLock("Follow Player");
            McClient.dispatcher.Unregister(CommandName);
            McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
        }

        private int OnCommandHelp(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>   Translations.cmd_follow_desc + ": " + Translations.cmd_follow_usage
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandStart(CmdResult r, string name, bool takeRisk)
        {
            if (!IsValidName(name))
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_follow_invalid_name);

            var player = GetEntities().Values.ToList().Find(entity =>
                entity.Type == EntityType.Player
                && !string.IsNullOrEmpty(entity.Name)
                && entity.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (player == null)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_follow_invalid_player);

            if (!CanMoveThere(player.Location))
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_follow_cant_reach_player);

            if (_playerToFollow != null && _playerToFollow.Equals(name, StringComparison.OrdinalIgnoreCase))
                return r.SetAndReturn(CmdResult.Status.Fail,
                    string.Format(Translations.cmd_follow_already_following, _playerToFollow));

            var movementLock = BotMovementLock.Instance;
            if (movementLock is { IsLocked: true })
                return r.SetAndReturn(CmdResult.Status.Fail,
                    string.Format(Translations.bot_common_movement_lock_held, "Follow Player", movementLock.LockedBy));

            var result =
                string.Format(
                    _playerToFollow != null ? Translations.cmd_follow_switched : Translations.cmd_follow_started,
                    player.Name!);
            _playerToFollow = name.ToLower();

            switch (movementLock)
            {
                case { IsLocked: false }:
                    if (!movementLock.Lock("Follow Player"))
                    {
                        LogToConsole($"§§6§1§0Follow Player bot failed to obtain the movement lock for some reason!");
                        LogToConsole($"§§6§1§0Disable other bots who have movement mechanics, and try again!");
                        return r.SetAndReturn(CmdResult.Status.Fail);
                    }

                    break;
            }

            if (!takeRisk) return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_follow_note);
            _unsafeEnabled = true;
            return r.SetAndReturn(CmdResult.Status.Done,
                Translations.cmd_follow_note + '\n' + Translations.cmd_follow_unsafe_enabled);
        }

        private int OnCommandStop(CmdResult r)
        {
            if (_playerToFollow == null)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_follow_already_stopped);

            var movementLock = BotMovementLock.Instance;
            movementLock?.UnLock("Follow Player");
            _playerToFollow = null;

            return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_follow_stopping);
        }

        public override void Update()
        {
            _updateCounter++;
        }

        public override void OnEntityMove(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow == null || string.IsNullOrEmpty(entity.Name))
                return;

            if (_playerToFollow != entity.Name.ToLower())
                return;

            if (_updateCounter < Settings.DoubleToTick(Config.Update_Limit))
                return;

            _updateCounter = 0;

            if (!CanMoveThere(entity.Location))
                return;

            // Stop at specified distance from player (prevents pushing player around)
            var distance = entity.Location.Distance(GetCurrentLocation());

            if (distance < Config.Stop_At_Distance)
                return;

            MoveToLocation(entity.Location, _unsafeEnabled);
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow != null && !string.IsNullOrEmpty(entity.Name) &&
                _playerToFollow.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsole(string.Format(Translations.cmd_follow_player_came_to_the_range, _playerToFollow));
                LogToConsole(Translations.cmd_follow_resuming);
            }
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow != null && !string.IsNullOrEmpty(entity.Name) &&
                _playerToFollow.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsole(string.Format(Translations.cmd_follow_player_left_the_range, _playerToFollow));
                LogToConsole(Translations.cmd_follow_pausing);
            }
        }

        public override void OnPlayerLeave(Guid uuid, string? name)
        {
            if (_playerToFollow != null && !string.IsNullOrEmpty(name) &&
                _playerToFollow.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsole(string.Format(Translations.cmd_follow_player_left, _playerToFollow));
                LogToConsole(Translations.cmd_follow_stopping);
                _playerToFollow = null;
            }
        }

        private bool CanMoveThere(Location location)
        {
            var chunkColumn = GetWorld().GetChunkColumn(location);
            return chunkColumn != null && chunkColumn.FullyLoaded != false;
        }
    }
}