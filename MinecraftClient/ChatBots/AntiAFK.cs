using System;
using MinecraftClient.Mapping;
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
            [NonSerialized]
            private const string BotName = "AntiAFK";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.AntiAfk.Delay$")]
            public Range Delay = new(600);

            [TomlInlineComment("$config.ChatBot.AntiAfk.Command$")]
            public string Command = "/ping";

            [TomlInlineComment("$config.ChatBot.AntiAfk.Use_Terrain_Handling$")]
            public bool Use_Terrain_Handling = false;

            [TomlInlineComment("$config.ChatBot.AntiAfk.Walk_Range$")]
            public int Walk_Range = 5;

            [TomlInlineComment("$config.ChatBot.AntiAfk.Walk_Retries$")]
            public int Walk_Retries = 20;

            public void OnSettingUpdate()
            {
                if (Walk_Range <= 0)
                {
                    Walk_Range = 5;
                    LogToConsole(BotName, Translations.TryGet("bot.antiafk.invalid_walk_range"));
                }

                if (Delay.min > Delay.max)
                {
                    (Delay.min, Delay.max) = (Delay.max, Delay.min);
                    LogToConsole(BotName, Translations.TryGet("bot.antiafk.swapping"));
                }

                Command ??= string.Empty;
            }

            public struct Range
            {
                public int min, max;

                public Range(int value)
                {
                    value = Math.Max(value, 10);
                    min = max = value;
                }

                public Range(int min, int max)
                {
                    min = Math.Max(min, 10);
                    max = Math.Max(max, 10);
                    this.min = min;
                    this.max = max;
                }
            }
        }

        private int count;
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
                    LogToConsole(Translations.TryGet("bot.antiafk.not_using_terrain_handling"));
                }
            }
        }

        public override void Update()
        {
            count++;

            if (count == random.Next(Config.Delay.min, Config.Delay.max))
            {
                DoAntiAfkStuff();
                count = 0;
            }

        }

        private void DoAntiAfkStuff()
        {
            if (Config.Use_Terrain_Handling && GetTerrainEnabled())
            {
                Location currentLocation = GetCurrentLocation();
                Location goal;

                bool moved = false;
                bool useAlternativeMethod = false;
                int triesCounter = 0;

                while (!moved)
                {
                    if (triesCounter++ >= Config.Walk_Retries)
                    {
                        useAlternativeMethod = true;
                        break;
                    }

                    goal = GetRandomLocationWithinRangeXZ(currentLocation, Config.Walk_Range);

                    // Prevent getting the same location
                    while ((currentLocation.X == goal.X) && (currentLocation.Y == goal.Y) && (currentLocation.Z == goal.Z))
                    {
                        LogToConsole("Same location!, generating new one");
                        goal = GetRandomLocationWithinRangeXZ(currentLocation, Config.Walk_Range);
                    }

                    if (!Movement.CheckChunkLoading(GetWorld(), currentLocation, goal))
                    {
                        useAlternativeMethod = true;
                        break;
                    }
                    else moved = MoveToLocation(goal, allowUnsafe: false, allowDirectTeleport: false);
                }

                if (!useAlternativeMethod)
                {
                    // Solve the case when the bot was closed in 1x2, was sneaking, but then he was freed, this will make him not sneak anymore
                    previousSneakState = false;
                    Sneak(false);

                    return;
                }
            }

            SendText(Config.Command);
            Sneak(previousSneakState);
            previousSneakState = !previousSneakState;
            count = 0;
        }

        private Location GetRandomLocationWithinRangeXZ(Location currentLocation, int range)
        {
            return new Location(currentLocation.X + random.Next(range * -1, range), currentLocation.Y, currentLocation.Z + random.Next(range * -1, range));
        }
    }
}
