using System;
using System.Diagnostics;
using DiscordRPC;
using DiscordRPC.Logging;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Displays a Discord Rich Presence status showing the player's
    /// current Minecraft session information (server, health, dimension, etc.).
    /// Requires a Discord Application ID from https://discord.com/developers/applications
    /// </summary>
    public class DiscordRpc : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "DiscordRpc";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.DiscordRpc.ApplicationId$")]
            public string ApplicationId = string.Empty;

            [TomlInlineComment("$ChatBot.DiscordRpc.PresenceDetails$")]
            public string PresenceDetails = "Playing on {server_host}:{server_port}";

            [TomlInlineComment("$ChatBot.DiscordRpc.PresenceState$")]
            public string PresenceState = "{dimension} - HP: {health}/{max_health}";

            [TomlInlineComment("$ChatBot.DiscordRpc.LargeImageKey$")]
            public string LargeImageKey = "mcc_icon";

            [TomlInlineComment("$ChatBot.DiscordRpc.LargeImageText$")]
            public string LargeImageText = "Minecraft Console Client";

            [TomlInlineComment("$ChatBot.DiscordRpc.SmallImageKey$")]
            public string SmallImageKey = string.Empty;

            [TomlInlineComment("$ChatBot.DiscordRpc.SmallImageText$")]
            public string SmallImageText = string.Empty;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowServerAddress$")]
            public bool ShowServerAddress = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowCoordinates$")]
            public bool ShowCoordinates = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowHealth$")]
            public bool ShowHealth = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowDimension$")]
            public bool ShowDimension = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowGamemode$")]
            public bool ShowGamemode = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowElapsedTime$")]
            public bool ShowElapsedTime = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowPlayerCount$")]
            public bool ShowPlayerCount = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.UpdateIntervalSeconds$")]
            public int UpdateIntervalSeconds = 10;

            public void OnSettingUpdate()
            {
                ApplicationId ??= string.Empty;
                PresenceDetails ??= string.Empty;
                PresenceState ??= string.Empty;
                LargeImageKey ??= string.Empty;
                LargeImageText ??= string.Empty;
                SmallImageKey ??= string.Empty;
                SmallImageText ??= string.Empty;

                if (UpdateIntervalSeconds < 1)
                {
                    UpdateIntervalSeconds = 10;
                    LogToConsole(BotName, Translations.bot_DiscordRpc_invalid_interval);
                }
            }
        }

        private DiscordRpcClient? rpcClient;
        private int tickCounter;
        private int updateIntervalTicks;
        private Timestamps? sessionTimestamps;
        private float lastHealth;

        public override void Initialize()
        {
            if (string.IsNullOrWhiteSpace(Config.ApplicationId))
            {
                LogToConsole(Translations.bot_DiscordRpc_missing_app_id);
                UnloadBot();
                return;
            }

            try
            {
                rpcClient = new DiscordRpcClient(Config.ApplicationId.Trim())
                {
                    Logger = Settings.Config.Logging.DebugMessages
                        ? new ConsoleLogger(LogLevel.Trace)
                        : new ConsoleLogger(LogLevel.None)
                };

                rpcClient.OnReady += (_, e) =>
                {
                    LogToConsole(string.Format(Translations.bot_DiscordRpc_connected, e.User.Username));
                };

                rpcClient.OnConnectionFailed += (_, e) =>
                {
                    LogToConsole(string.Format(Translations.bot_DiscordRpc_connection_failed, e.FailedPipe));
                };

                rpcClient.Initialize();
                updateIntervalTicks = Settings.DoubleToTick(Config.UpdateIntervalSeconds);

                if (Config.ShowElapsedTime)
                    sessionTimestamps = Timestamps.Now;

                lastHealth = Handler.GetHealth();

                SetPresence();
                LogToConsole(Translations.bot_DiscordRpc_initialized);
            }
            catch (Exception e)
            {
                LogToConsole(string.Format(Translations.bot_DiscordRpc_init_error, e.Message));
                LogDebugToConsole(e.StackTrace ?? string.Empty);
                UnloadBot();
            }
        }

        public override void OnUnload()
        {
            if (rpcClient is { IsDisposed: false })
            {
                rpcClient.ClearPresence();
                rpcClient.Dispose();
            }

            rpcClient = null;
        }

        public override void AfterGameJoined()
        {
            if (Config.ShowElapsedTime)
                sessionTimestamps = Timestamps.Now;

            SetPresence();
        }

        public override void Update()
        {
            tickCounter++;
            if (tickCounter < updateIntervalTicks)
                return;

            tickCounter = 0;
            SetPresence();
        }

        public override void OnHealthUpdate(float health, int food)
        {
            lastHealth = health;
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            if (rpcClient is { IsDisposed: false })
                rpcClient.ClearPresence();

            return false;
        }

        private void SetPresence()
        {
            if (rpcClient is null or { IsDisposed: true })
                return;

            try
            {
                string details = ReplacePlaceholders(Config.PresenceDetails);
                string state = ReplacePlaceholders(Config.PresenceState);

                var presence = new RichPresence
                {
                    Details = TruncateForDiscord(details, 128),
                    State = TruncateForDiscord(state, 128)
                };

                // Assets (images)
                var assets = new Assets();
                bool hasAssets = false;

                if (!string.IsNullOrWhiteSpace(Config.LargeImageKey))
                {
                    assets.LargeImageKey = Config.LargeImageKey.Trim();
                    assets.LargeImageText = TruncateForDiscord(
                        ReplacePlaceholders(Config.LargeImageText), 128);
                    hasAssets = true;
                }

                if (!string.IsNullOrWhiteSpace(Config.SmallImageKey))
                {
                    assets.SmallImageKey = Config.SmallImageKey.Trim();
                    assets.SmallImageText = TruncateForDiscord(
                        ReplacePlaceholders(Config.SmallImageText), 128);
                    hasAssets = true;
                }

                if (hasAssets)
                    presence.Assets = assets;

                // Timestamps
                if (Config.ShowElapsedTime && sessionTimestamps is not null)
                    presence.Timestamps = sessionTimestamps;

                // Player count as party
                if (Config.ShowPlayerCount)
                {
                    string[] onlinePlayers = GetOnlinePlayers();
                    int playerCount = onlinePlayers.Length;
                    if (playerCount > 0)
                    {
                        presence.Party = new Party
                        {
                            ID = $"mcc_{GetServerHost()}_{GetServerPort()}",
                            Size = playerCount,
                            Max = playerCount
                        };
                    }
                }

                rpcClient.SetPresence(presence);
            }
            catch (Exception e)
            {
                LogDebugToConsole(string.Format(Translations.bot_DiscordRpc_update_error, e.Message));
            }
        }

        private string ReplacePlaceholders(string template)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string serverHost = Config.ShowServerAddress ? GetServerHost() : "Hidden";
            int serverPort = GetServerPort();
            string serverPortStr = Config.ShowServerAddress ? serverPort.ToString() : "****";
            string username = GetUsername();
            float health = Handler.GetHealth();
            int foodLevel = Handler.GetSaturation();
            Location location = GetCurrentLocation();
            string[] onlinePlayers = GetOnlinePlayers();
            int gamemode = GetGamemode();
            int protocolVersion = GetProtocolVersion();

            string healthStr = Config.ShowHealth ? ((int)Math.Ceiling(health)).ToString() : "?";
            string maxHealthStr = Config.ShowHealth ? "20" : "?";
            string foodStr = Config.ShowHealth ? foodLevel.ToString() : "?";
            string xStr = Config.ShowCoordinates ? ((int)location.X).ToString() : "?";
            string yStr = Config.ShowCoordinates ? ((int)location.Y).ToString() : "?";
            string zStr = Config.ShowCoordinates ? ((int)location.Z).ToString() : "?";

            string dimensionName = Config.ShowDimension ? "Unknown" : "Hidden";
            if (Config.ShowDimension)
            {
                try
                {
                    var dim = World.GetDimension();
                    dimensionName = dim.Name ?? "Unknown";

                    // Clean up the dimension name for display
                    if (dimensionName.StartsWith("minecraft:"))
                        dimensionName = dimensionName["minecraft:".Length..];

                    dimensionName = dimensionName switch
                    {
                        "overworld" => "Overworld",
                        "the_nether" => "The Nether",
                        "the_end" => "The End",
                        _ => dimensionName
                    };
                }
                catch
                {
                    // World may not be available
                }
            }

            string gamemodeStr = Config.ShowGamemode
                ? gamemode switch
                {
                    0 => "Survival",
                    1 => "Creative",
                    2 => "Adventure",
                    3 => "Spectator",
                    _ => "Unknown"
                }
                : "Hidden";

            return template
                .Replace("{server_host}", serverHost)
                .Replace("{server_port}", serverPortStr)
                .Replace("{username}", username)
                .Replace("{health}", healthStr)
                .Replace("{max_health}", maxHealthStr)
                .Replace("{food}", foodStr)
                .Replace("{dimension}", dimensionName)
                .Replace("{gamemode}", gamemodeStr)
                .Replace("{x}", xStr)
                .Replace("{y}", yStr)
                .Replace("{z}", zStr)
                .Replace("{player_count}", onlinePlayers.Length.ToString())
                .Replace("{protocol}", protocolVersion.ToString());
        }

        private static string TruncateForDiscord(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
        }
    }
}
