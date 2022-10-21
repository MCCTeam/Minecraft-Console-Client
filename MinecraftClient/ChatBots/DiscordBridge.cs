using System;
using System.Collections.Generic;
using System.IO;
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
        private static DiscordBridge? instance = null;

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

        public DiscordBridge()
        {
            instance = this;
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

        public static DiscordBridge? GetInstance()
        {
            return instance;
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

                SendMessage(messageBuilder);
                return;
            }
            else SendMessage(message);
        }

        public void SendMessage(string message)
        {
            if (_client == null || _channel == null)
                return;

            _client.SendMessageAsync(_channel, message).Wait();
        }

        public void SendMessage(DiscordMessageBuilder builder)
        {
            if (_client == null || _channel == null)
                return;

            _client.SendMessageAsync(_channel, builder).Wait();
        }

        public void SendMessage(DiscordEmbedBuilder embedBuilder)
        {
            if (_client == null || _channel == null)
                return;

            _client.SendMessageAsync(_channel, embedBuilder).Wait();

        }
        public void SendImage(string filePath, string? text = null)
        {
            if (_client == null || _channel == null)
                return;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                filePath = filePath[(filePath.IndexOf(Path.DirectorySeparatorChar) + 1)..];
                var messageBuilder = new DiscordMessageBuilder().WithContent(text);

                messageBuilder.WithFiles(new Dictionary<string, Stream>() { { $"attachment://{filePath}", fs } });

                _client.SendMessageAsync(_channel, messageBuilder).Wait();
            }
        }

        public void SendFile(FileStream fileStream)
        {
            if (_client == null || _channel == null)
                return;

            var messageBuilder = new DiscordMessageBuilder().WithFile(fileStream);
            SendMessage(messageBuilder);
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
                    MinimumLogLevel = Settings.Config.Logging.DebugMessages ?
                        (LogLevel.Trace | LogLevel.Information | LogLevel.Debug | LogLevel.Critical | LogLevel.Error | LogLevel.Warning) : LogLevel.None
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

                    LogDebugToConsole("Exception when trying to find the guild:");
                    LogDebugToConsole(e);
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

                    LogDebugToConsole("Exception when trying to find the channel:");
                    LogDebugToConsole(e);
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
