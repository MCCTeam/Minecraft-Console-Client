using System;
using MinecraftClient.Mapping;
using MinecraftClient.Mcp;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class McpServer : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "McpServer";

            [TomlInlineComment("$ChatBot.McpServer.Enabled$")]
            public bool Enabled = false;

            [TomlPrecedingComment("$ChatBot.McpServer.Transport$")]
            public MccMcpTransportConfig Transport = new();

            [TomlPrecedingComment("$ChatBot.McpServer.Capabilities$")]
            public MccMcpCapabilityToggles Capabilities = new();

            public void OnSettingUpdate()
            {
                Transport ??= new MccMcpTransportConfig();
                Capabilities ??= new MccMcpCapabilityToggles();

                if (Transport.Port is < 1 or > 65535)
                    Transport.Port = 33333;

                if (string.IsNullOrWhiteSpace(Transport.BindHost))
                    Transport.BindHost = "127.0.0.1";

                if (string.IsNullOrWhiteSpace(Transport.Route))
                    Transport.Route = "/mcp";

                if (!Transport.Route.StartsWith('/'))
                    Transport.Route = "/" + Transport.Route;

                if (string.IsNullOrWhiteSpace(Transport.AuthTokenEnvVar))
                    Transport.AuthTokenEnvVar = "MCC_MCP_AUTH_TOKEN";
            }
        }

        private MccEmbeddedMcpHost? host;

        public override void Initialize()
        {
            Config.OnSettingUpdate();
        }

        public override void AfterGameJoined()
        {
            if (!Config.Enabled)
                return;

            ClearStores();

            MccMcpConfig mcpConfig = new()
            {
                Enabled = Config.Enabled,
                Transport = Config.Transport,
                Capabilities = Config.Capabilities
            };

            host ??= new MccEmbeddedMcpHost(mcpConfig, new MccMcpCapabilities(() => Config.Capabilities));

            if (host.IsRunning)
                return;

            LogToConsole(Translations.bot_mcpserver_starting);
            if (!host.Start(out string? error))
            {
                if (error == "missing_auth_token")
                    LogToConsole(string.Format(Translations.bot_mcpserver_missing_auth_token, Config.Transport.AuthTokenEnvVar));
                LogToConsole(string.Format(Translations.bot_mcpserver_start_failed, error ?? "unknown"));
                return;
            }

            LogToConsole(string.Format(Translations.bot_mcpserver_started, host.Endpoint));
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            MccMcpRecentEventStore.Add("disconnect", new
            {
                reason = reason.ToString(),
                message
            });
            StopHost();
            ClearStores();
            return false;
        }

        public override void OnUnload()
        {
            StopHost();
            ClearStores();
        }

        public override void GetText(string text, string? json)
        {
            string clean = GetVerbatim(text);
            if (string.IsNullOrWhiteSpace(clean))
                return;

            string kind = "system";
            string? sender = null;
            string? message = null;

            string parsedMessage = string.Empty;
            string parsedSender = string.Empty;
            if (IsPrivateMessage(clean, ref parsedMessage, ref parsedSender))
            {
                kind = "private";
                sender = parsedSender;
                message = parsedMessage;
            }
            else if (IsChatMessage(clean, ref parsedMessage, ref parsedSender))
            {
                kind = "chat";
                sender = parsedSender;
                message = parsedMessage;
            }

            MccMcpChatHistoryStore.Add(new MccMcpChatHistoryEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Kind = kind,
                Text = clean,
                Sender = sender,
                Message = message,
                Json = json
            });
        }

        public override void OnTimeUpdate(long WorldAge, long TimeOfDay)
        {
            MccMcpRuntimeStateStore.SetTime(WorldAge, TimeOfDay);
        }

        public override void OnRainLevelChange(float level)
        {
            MccMcpRuntimeStateStore.SetRainLevel(level);
            MccMcpRecentEventStore.Add("weather_rain", new { level });
        }

        public override void OnThunderLevelChange(float level)
        {
            MccMcpRuntimeStateStore.SetThunderLevel(level);
            MccMcpRecentEventStore.Add("weather_thunder", new { level });
        }

        public override void OnDeath()
        {
            MccMcpRecentEventStore.Add("death");
        }

        public override void OnRespawn()
        {
            MccMcpRecentEventStore.Add("respawn");
        }

        public override void OnPlayerJoin(Guid uuid, string name)
        {
            MccMcpRecentEventStore.Add("player_join", new
            {
                uuid,
                name
            });
        }

        public override void OnPlayerLeave(Guid uuid, string? name)
        {
            MccMcpRecentEventStore.Add("player_leave", new
            {
                uuid,
                name
            });
        }

        public override void OnInventoryOpen(int inventoryId)
        {
            MccMcpRecentEventStore.Add("inventory_open", new { inventoryId });
        }

        public override void OnInventoryClose(int inventoryId)
        {
            MccMcpRecentEventStore.Add("inventory_close", new { inventoryId });
        }

        public override void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json)
        {
            if (action == 2)
            {
                MccMcpRecentEventStore.Add("actionbar", new
                {
                    action,
                    text = actionbartext,
                    fadein,
                    stay,
                    fadeout,
                    json
                });
                return;
            }

            if (action is 0 or 1)
            {
                MccMcpRecentEventStore.Add("title", new
                {
                    action,
                    titleText = titletext,
                    subtitleText = subtitletext,
                    fadein,
                    stay,
                    fadeout,
                    json
                });
            }
        }

        public override void OnBlockBreakAnimation(Entity entity, Location location, byte stage)
        {
            MccMcpRecentEventStore.Add("block_break_animation", new
            {
                entityId = entity.ID,
                entityType = entity.Type.ToString(),
                stage,
                location = new
                {
                    x = location.X,
                    y = location.Y,
                    z = location.Z
                }
            });
        }

        public override void OnEntityAnimation(Entity entity, byte animation)
        {
            MccMcpRecentEventStore.Add("entity_animation", new
            {
                entityId = entity.ID,
                entityType = entity.Type.ToString(),
                animation,
                name = entity.Name,
                customName = entity.CustomName
            });
        }

        private void StopHost()
        {
            if (host is null || !host.IsRunning)
                return;

            if (host.Stop(out string? error))
                LogToConsole(Translations.bot_mcpserver_stopped);
            else
                LogToConsole(string.Format(Translations.bot_mcpserver_stop_failed, error ?? "unknown"));
        }

        private static void ClearStores()
        {
            MccMcpChatHistoryStore.Clear();
            MccMcpRuntimeStateStore.Clear();
            MccMcpRecentEventStore.Clear();
        }
    }
}
