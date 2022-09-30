using System;
using System.Linq;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    public class FollowPlayer : ChatBot
    {
        private string? _playerToFollow = null;
        private int _updateCounter = 0;
        private readonly int _updateLimit;
        private readonly int _stopAtDistance;
        private bool _unsafeEnabled = false;

        public FollowPlayer(int updateLimit = 15, int stopAtDistance = 3)
        {
            _updateLimit = updateLimit;
            _stopAtDistance = stopAtDistance;
        }

        public override void Initialize()
        {
            if (!GetEntityHandlingEnabled())
            {
                LogToConsoleTranslated("extra.entity_required");
                LogToConsoleTranslated("general.bot_unload");
                UnloadBot();
                return;
            }

            if (!GetTerrainEnabled())
            {
                LogToConsoleTranslated("extra.terrainandmovement_required");
                LogToConsoleTranslated("general.bot_unload");
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
                        return Translations.TryGet("cmd.follow.already_stopped");

                    _playerToFollow = null;
                    return Translations.TryGet("cmd.follow.stopping");
                }
                else
                {
                    if (!IsValidName(args[0]))
                        return Translations.TryGet("cmd.follow.invalid_name");

                    Entity? player = GetEntities().Values.ToList().Find(entity =>
                        entity.Type == EntityType.Player && !string.IsNullOrEmpty(entity.Name) && entity.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

                    if (player == null)
                        return Translations.TryGet("cmd.follow.invalid_player");

                    if (!CanMoveThere(player.Location))
                        return Translations.TryGet("cmd.follow.cant_reach_player");

                    if (_playerToFollow != null && _playerToFollow.Equals(args[0], StringComparison.OrdinalIgnoreCase))
                        return Translations.TryGet("cmd.follow.already_following", _playerToFollow);

                    string result = Translations.TryGet(_playerToFollow != null ? "cmd.follow.switched" : "cmd.follow.started", player.Name!);
                    _playerToFollow = args[0].Trim().ToLower();

                    LogToConsoleTranslated("cmd.follow.note");

                    if (args.Length == 2 && args[1].Equals("-f", StringComparison.OrdinalIgnoreCase))
                    {
                        _unsafeEnabled = true;
                        LogToConsoleTranslated("cmd.follow.unsafe_enabled");
                    }

                    return result;
                }
            }

            return Translations.TryGet("cmd.follow.desc") + ": " + Translations.TryGet("cmd.follow.usage");
        }

        public override void Update()
        {
            _updateCounter++;
        }

        public override void OnEntityMove(Entity entity)
        {

            if (_updateCounter < _updateLimit)
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

            if (distance < _stopAtDistance)
                return;

            MoveToLocation(entity.Location, _unsafeEnabled);
        }

        public override void OnEntitySpawn(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow != null && !string.IsNullOrEmpty(entity.Name) && _playerToFollow.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsoleTranslated("cmd.follow.player_came_to_the_range", _playerToFollow);
                LogToConsoleTranslated("cmd.follow.resuming");
            }
        }

        public override void OnEntityDespawn(Entity entity)
        {
            if (entity.Type != EntityType.Player)
                return;

            if (_playerToFollow != null && !string.IsNullOrEmpty(entity.Name) && _playerToFollow.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsoleTranslated("cmd.follow.player_left_the_range", _playerToFollow);
                LogToConsoleTranslated("cmd.follow.pausing");
            }
        }

        public override void OnPlayerLeave(Guid uuid, string? name)
        {
            if (_playerToFollow != null && !string.IsNullOrEmpty(name) && _playerToFollow.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                LogToConsoleTranslated("cmd.follow.player_left", _playerToFollow);
                LogToConsoleTranslated("cmd.follow.stopping");
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