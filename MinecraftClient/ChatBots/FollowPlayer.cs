using System;
using System.Linq;
using MinecraftClient.Mapping;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class FollowPlayer : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "FollowPlayer";

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

            RegisterChatBotCommand("follow", "cmd.follow.desc", "follow <player name|stop>", OnFollowCommand);
        }

        private string OnFollowCommand(string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    if (_playerToFollow == null)
                        return Translations.cmd_follow_already_stopped;

                    _playerToFollow = null;
                    return Translations.cmd_follow_stopping;
                }
                else
                {
                    if (!IsValidName(args[0]))
                        return Translations.cmd_follow_invalid_name;

                    Entity? player = GetEntities().Values.ToList().Find(entity =>
                        entity.Type == EntityType.Player && !string.IsNullOrEmpty(entity.Name) && entity.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

                    if (player == null)
                        return Translations.cmd_follow_invalid_player;

                    if (!CanMoveThere(player.Location))
                        return Translations.cmd_follow_cant_reach_player;

                    if (_playerToFollow != null && _playerToFollow.Equals(args[0], StringComparison.OrdinalIgnoreCase))
                        return string.Format(Translations.cmd_follow_already_following, _playerToFollow);

                    string result;
                    if (_playerToFollow != null)
                        result = string.Format(Translations.cmd_follow_switched, player.Name!);
                    else
                        result = string.Format(Translations.cmd_follow_started, player.Name!);
                    _playerToFollow = args[0].Trim().ToLower();

                    LogToConsole(Translations.cmd_follow_note);

                    if (args.Length == 2 && args[1].Equals("-f", StringComparison.OrdinalIgnoreCase))
                    {
                        _unsafeEnabled = true;
                        LogToConsole(Translations.cmd_follow_unsafe_enabled);
                    }

                    return result;
                }
            }

            return Translations.cmd_follow_desc + ": " + Translations.cmd_follow_usage;
        }

        public override void Update()
        {
            _updateCounter++;
        }

        public override void OnEntityMove(Entity entity)
        {

            if (_updateCounter < Settings.DoubleToTick(Config.Update_Limit))
                return;

            _updateCounter = 0;

            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow == null || string.IsNullOrEmpty(entity.Name))
                return;

            if (_playerToFollow != entity.Name.ToLower())
                return;

            if (!CanMoveThere(entity.Location))
                return;

            // Stop at specified distance from plater (prevents pushing player around)
            double distance = entity.Location.Distance(GetCurrentLocation());

            if (distance < Config.Stop_At_Distance)
                return;

            MoveToLocation(entity.Location, _unsafeEnabled);
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow != null && !string.IsNullOrEmpty(entity.Name) && _playerToFollow.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsole(string.Format(Translations.cmd_follow_player_came_to_the_range, _playerToFollow));
                LogToConsole(Translations.cmd_follow_resuming);
            }
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow != null && !string.IsNullOrEmpty(entity.Name) && _playerToFollow.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsole(string.Format(Translations.cmd_follow_player_left_the_range, _playerToFollow));
                LogToConsole(Translations.cmd_follow_pausing);
            }
        }

        public override void OnPlayerLeave(Guid uuid, string? name)
        {
            if (_playerToFollow != null && !string.IsNullOrEmpty(name) && _playerToFollow.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsole(string.Format(Translations.cmd_follow_player_left, _playerToFollow));
                LogToConsole(Translations.cmd_follow_stopping);
                _playerToFollow = null;
            }
        }

        private bool CanMoveThere(Location location)
        {
            ChunkColumn? chunkColumn = GetWorld().GetChunkColumn(location);

            if (chunkColumn == null || chunkColumn.FullyLoaded == false)
                return false;

            return true;
        }
    }
}