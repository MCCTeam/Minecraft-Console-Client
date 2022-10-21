using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class DiscordBridge : ChatBot
    {
        private DiscordClient? _client;
        private DiscordChannel? _channel;

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "DiscordBridge";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.DiscordBridge.Token$")]
            public string Token = "your bot token here";

            [TomlInlineComment("$config.ChatBot.DiscordBridge.GuildId$")]
            public ulong GuildId = 1018553894831403028L;

            [TomlInlineComment("$config.ChatBot.DiscordBridge.ChannelId$")]
            public ulong ChannelId = 1018565295654326364L;

            [TomlInlineComment("$config.ChatBot.DiscordBridge.OwnersIds$")]
            public ulong[] OwnersIds = new[] { 978757810781323276UL };

            [TomlPrecedingComment("$config.ChatBot.DiscordBridge.Formats$")]
            public string PrivateMessageFormat = "**[Private Message]** {username}: {message}";
            public string PublicMessageFormat = "{username}: {message}";
            public string TeleportRequestMessageFormat = "A new Teleport Request from **{username}**!";

            public void OnSettingUpdate() { }
        }

        public override void Initialize()
        {
            Task.Run(async () => await MainAsync());
        }

        ~DiscordBridge()
        {
            Disconnect();
        }

        public override void OnUnload()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (_client != null)
            {
                if (_channel != null)
                    _client.SendMessageAsync(_channel, new DiscordEmbedBuilder
                    {
                        Description = Translations.TryGet("bot.DiscordBridge.disconnected"),
                        Color = new DiscordColor(0xFF0000)
                    }).Wait();

                _client.DisconnectAsync().Wait();
            }
        }

        public override void GetText(string text)
        {
            if (_client == null || _channel == null)
                return;

            text = GetVerbatim(text).Trim();

            string message = "";
            string username = "";
            bool teleportRequest = false;

            if (IsPrivateMessage(text, ref message, ref username))
                message = Config.PrivateMessageFormat.Replace("{username}", username).Replace("{message}", message).Replace("{timestamp}", GetTimestamp()).Trim();
            else if (IsChatMessage(text, ref message, ref username))
                message = Config.PublicMessageFormat.Replace("{username}", username).Replace("{message}", message).Replace("{timestamp}", GetTimestamp()).Trim();
            else if (IsTeleportRequest(text, ref username))
            {
                message = Config.TeleportRequestMessageFormat.Replace("{username}", username).Replace("{timestamp}", GetTimestamp()).Trim();
                teleportRequest = true;
            }
            else message = text;

            SendMessageToDiscord(message, teleportRequest);
        }

        private void SendMessageToDiscord(string message, bool teleportRequest = false)
        {
            if (_client == null || _channel == null)
                return;

            if (teleportRequest)
            {
                var messageBuilder = new DiscordMessageBuilder()
                    .WithEmbed(new DiscordEmbedBuilder
                    {
                        Description = message,
                        Color = new DiscordColor(0x3399FF)
                    })
                    .AddComponents(new DiscordComponent[]{
                        new DiscordButtonComponent(ButtonStyle.Success, "accept_teleport", "Accept"),
                        new DiscordButtonComponent(ButtonStyle.Danger, "deny_teleport", "Deny")
                    });

                _client.SendMessageAsync(_channel, messageBuilder).Wait();
                return;
            }

            _client.SendMessageAsync(_channel, message).Wait();
        }

        async Task MainAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Config.Token.Trim()))
                {
                    LogToConsole(Translations.TryGet("bot.DiscordBridge.missing_token"));
                    UnloadBot();
                    return;
                }

                _client = new DiscordClient(new DiscordConfiguration()
                {
                    Token = Config.Token.Trim(),
                    TokenType = TokenType.Bot,
                    AutoReconnect = true,
                    Intents = DiscordIntents.All,
                    MinimumLogLevel = LogLevel.None
                });

                try
                {
                    await _client.GetGuildAsync(Config.GuildId);
                }
                catch (Exception e)
                {
                    if (e is NotFoundException)
                    {
                        LogToConsole(Translations.TryGet("bot.DiscordBridge.guild_not_found", Config.GuildId));
                        UnloadBot();
                        return;
                    }
                }

                try
                {
                    _channel = await _client.GetChannelAsync(Config.ChannelId);
                }
                catch (Exception e)
                {
                    if (e is NotFoundException)
                    {
                        LogToConsole(Translations.TryGet("bot.DiscordBridge.channel_not_found", Config.ChannelId));
                        UnloadBot();
                        return;
                    }
                }

                _client.MessageCreated += async (source, e) =>
                {
                    if (e.Guild.Id != Config.GuildId)
                        return;

                    if (e.Channel.Id != Config.ChannelId)
                        return;

                    if (!Config.OwnersIds.Contains(e.Author.Id))
                        return;

                    string message = e.Message.Content.Trim();

                    if (message.StartsWith("."))
                    {
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromName(_client, ":gear:"));
                        message = message[1..];

                        string? result = "";
                        PerformInternalCommand(message, ref result);
                        result = string.IsNullOrEmpty(result) ? "-" : result;

                        await e.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(_client, ":gear:"));
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromName(_client, ":white_check_mark:"));
                        await e.Message.RespondAsync($"{Translations.TryGet("bot.DiscordBridge.command_executed")}:\n```{result}```");
                    }
                    else SendText(message);
                };

                _client.ComponentInteractionCreated += async (s, e) =>
                {
                    if (!(e.Id.Equals("accept_teleport") || e.Id.Equals("deny_teleport")))
                        return;

                    string result = e.Id.Equals("accept_teleport") ? "Accepted :white_check_mark:" : "Denied :x:";
                    SendText(e.Id.Equals("accept_teleport") ? "/tpaccept" : "/tpdeny");
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(result));
                };

                await _client.ConnectAsync();

                await _client.SendMessageAsync(_channel, new DiscordEmbedBuilder
                {
                    Description = Translations.TryGet("bot.DiscordBridge.connected"),
                    Color = new DiscordColor(0x00FF00)
                });

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                LogToConsole(Translations.TryGet("bot.DiscordBridge.unknown_error"));
                LogToConsole(e);
                return;
            }
        }
    }
}
