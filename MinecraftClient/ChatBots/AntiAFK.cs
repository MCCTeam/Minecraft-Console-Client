using System;
using MinecraftClient.Mapping;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot sends a command every 60 seconds in order to stay non-afk.
    /// </summary>

    public class AntiAFK : ChatBot
    {
        private int count;
        private readonly string pingparam;
        private int timeping = 600;
        private int timepingMax = -1;
        private bool useTerrainHandling = false;
        private bool previousSneakState = false;
        private int walkRange = 5;
        private readonly int walkRetries = 10;
        private readonly Random random = new();

        /// <summary>
        /// This bot sends a /ping command every X seconds in order to stay non-afk.
        /// </summary>
        /// <param name="pingparam">Time amount between each ping (10 = 1s, 600 = 1 minute, etc.) Can be a range of numbers eg. 10-600</param>

        public AntiAFK(string pingparam, bool useTerrainHandling, int walkRange, int walkRetries)
        {
            count = 0;
            this.pingparam = pingparam;
            this.useTerrainHandling = useTerrainHandling;
            this.walkRange = walkRange;
            this.walkRetries = walkRetries;
        }

        public override void Initialize()
        {
            if (useTerrainHandling)
            {
                if (!GetTerrainEnabled())
                {
                    useTerrainHandling = false;
                    LogToConsole(Translations.TryGet("bot.antiafk.not_using_terrain_handling"));
                }
                else
                {
                    if (walkRange <= 0)
                    {
                        walkRange = 5;
                        LogToConsole(Translations.TryGet("bot.antiafk.invalid_walk_range"));
                    }
                }
            }

            if (string.IsNullOrEmpty(pingparam))
                LogToConsole(Translations.TryGet("bot.antiafk.invalid_time"));
            else
            {
                // Handle the random range
                if (pingparam.Contains('-'))
                {
                    string[] parts = pingparam.Split("-");

                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out int firstTime))
                        {
                            timeping = firstTime;

                            if (int.TryParse(parts[1].Trim(), out int secondTime))
                                timepingMax = secondTime;
                            else LogToConsole(Translations.TryGet("bot.antiafk.invalid_range_partial", timeping));
                        }
                        else LogToConsole(Translations.TryGet("bot.antiafk.invalid_range"));
                    }
                    else LogToConsole(Translations.TryGet("bot.antiafk.invalid_range"));
                }
                else
                {
                    if (int.TryParse(pingparam.Trim(), out int value))
                        timeping = value;
                    else LogToConsole(Translations.TryGet("bot.antiafk.invalid_value"));
                }
            }

            if (timepingMax != -1 && timeping > timepingMax)
            {
                (timeping, timepingMax) = (timepingMax, timeping);
                LogToConsole(Translations.TryGet("bot.antiafk.swapping"));
            }

            if (timeping < 10) { timeping = 10; } //To avoid flooding
        }

        public override void Update()
        {
            count++;

            if ((timepingMax != -1 && count == random.Next(timeping, timepingMax)) || count == timeping)
            {
                DoAntiAfkStuff();
                count = 0;
            }

        }

        private void DoAntiAfkStuff()
        {
            if (useTerrainHandling)
            {
                Location currentLocation = GetCurrentLocation();
                Location goal;

                bool moved = false;
                bool useAlternativeMethod = false;
                int triesCounter = 0;

                while (!moved)
                {
                    if (triesCounter++ >= walkRetries)
                    {
                        useAlternativeMethod = true;
                        break;
                    }

                    goal = GetRandomLocationWithinRangeXZ(currentLocation, walkRange);

                    // Prevent getting the same location
                    while ((currentLocation.X == goal.X) && (currentLocation.Y == goal.Y) && (currentLocation.Z == goal.Z))
                    {
                        LogToConsole("Same location!, generating new one");
                        goal = GetRandomLocationWithinRangeXZ(currentLocation, walkRange);
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

            SendText(Settings.AntiAFK_Command);
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
