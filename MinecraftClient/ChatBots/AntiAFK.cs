using System;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot sends a command every 60 seconds in order to stay non-afk.
    /// </summary>
    public class AntiAFK : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized] private const string BotName = "AntiAFK";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AntiAfk.Delay$")]
            public Range Delay = new(60);

            [TomlInlineComment("$ChatBot.AntiAfk.Command$")]
            public string Command = "/ping";

            [TomlInlineComment("$ChatBot.AntiAfk.Use_Sneak$")]
            public bool Use_Sneak = false;

            [TomlInlineComment("$ChatBot.AntiAfk.Use_Terrain_Handling$")]
            public bool Use_Terrain_Handling = false;

            [TomlInlineComment("$ChatBot.AntiAfk.Walk_Range$")]
            public int Walk_Range = 5;

            [TomlInlineComment("$ChatBot.AntiAfk.Walk_Retries$")]
            public int Walk_Retries = 20;

            public void OnSettingUpdate()
            {
                if (Walk_Range <= 0)
                {
                    Walk_Range = 5;
                    LogToConsole(BotName, Translations.bot_antiafk_invalid_walk_range);
                }

                Delay.min = Math.Max(1.0, Delay.min);
                Delay.max = Math.Max(1.0, Delay.max);

                Delay.min = Math.Min(int.MaxValue / 10, Delay.min);
                Delay.max = Math.Min(int.MaxValue / 10, Delay.max);

                if (Delay.min > Delay.max)
                {
                    (Delay.min, Delay.max) = (Delay.max, Delay.min);
                    LogToConsole(BotName, Translations.bot_antiafk_swapping);
                }

                Command ??= string.Empty;
            }

            public struct Range
            {
                public double min, max;

                public Range(int value)
                {
                    min = max = value;
                }

                public Range(int min, int max)
                {
                    this.min = min;
                    this.max = max;
                }
            }
        }

        private int count, nextrun = 50;
        private bool previousSneakState = false;
        private readonly Random random = new();

        /// <summary>
        /// This bot sends a /ping command every X seconds in order to stay non-afk.
        /// </summary>
        public AntiAFK()
        {
            count = 0;
        }

        public override void Initialize()
        {
            if (Config.Use_Terrain_Handling)
            {
                if (!GetTerrainEnabled())
                {
                    LogToConsole(Translations.bot_antiafk_not_using_terrain_handling);
                }
                else
                {
                    var movementLock = BotMovementLock.Instance;
                    if (movementLock is { IsLocked: true })
                        LogToConsole(
                            $"§§6§1§0{string.Format(Translations.bot_antiafk_may_not_move, movementLock.LockedBy)}");
                }
            }
        }

        public override void Update()
        {
            count++;

            if (count < nextrun) return;
            DoAntiAfkStuff();
            count = 0;
            nextrun = random.Next(Settings.DoubleToTick(Config.Delay.min), Settings.DoubleToTick(Config.Delay.max));
        }

        private void DoAntiAfkStuff()
        {
            var isMovementLocked = BotMovementLock.Instance;
            if (Config.Use_Terrain_Handling && GetTerrainEnabled() && isMovementLocked is {IsLocked: false})
            {
                var currentLocation = GetCurrentLocation();

                var moved = false;
                var useAlternativeMethod = false;
                var triesCounter = 0;

                while (!moved)
                {
                    if (triesCounter++ >= Config.Walk_Retries)
                    {
                        useAlternativeMethod = true;
                        break;
                    }

                    var goal = GetRandomLocationWithinRangeXZ(currentLocation, Config.Walk_Range);

                    // Prevent getting the same location
                    while ((currentLocation.X == goal.X) && (currentLocation.Y == goal.Y) &&
                           (currentLocation.Z == goal.Z))
                    {
                        LogToConsole("Same location!, generating new one");
                        goal = GetRandomLocationWithinRangeXZ(currentLocation, Config.Walk_Range);
                    }

                    if (!Movement.CheckChunkLoading(GetWorld(), currentLocation, goal))
                    {
                        useAlternativeMethod = true;
                        break;
                    }

                    moved = MoveToLocation(goal, allowUnsafe: false, allowDirectTeleport: false);
                }

                if (!useAlternativeMethod && Config.Use_Sneak)
                {
                    // Solve the case when the bot was closed in 1x2, was sneaking, but then he was freed, this will make him not sneak anymore
                    previousSneakState = false;
                    Sneak(false);
                    return;
                }
            }

            SendText(Config.Command);
            if (Config.Use_Sneak)
            {
                Sneak(previousSneakState);
                previousSneakState = !previousSneakState;
            }

            count = 0;
        }

        private Location GetRandomLocationWithinRangeXZ(Location currentLocation, int range)
        {
            return new Location(currentLocation.X + random.Next(range * -1, range), currentLocation.Y,
                currentLocation.Z + random.Next(range * -1, range));
        }
    }
}