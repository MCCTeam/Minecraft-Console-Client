using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Brigadier.NET.Builder;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class DiscordBridge : ChatBot
    {
        public const string CommandName = "dscbridge";

        private enum BridgeDirection
        {
            Both = 0,
            Minecraft,
            Discord
        }

        private static DiscordBridge? instance = null;
        public bool IsConnected { get; private set; }

        private DiscordClient? discordBotClient;
        private DiscordChannel? discordChannel;
        private BridgeDirection bridgeDirection = BridgeDirection.Both;

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "DiscordBridge";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.DiscordBridge.Token$")]
            public string Token = "your bot token here";

            [TomlInlineComment("$ChatBot.DiscordBridge.GuildId$")]
            public ulong GuildId = 1018553894831403028L;

            [TomlInlineComment("$ChatBot.DiscordBridge.ChannelId$")]
            public ulong ChannelId = 1018565295654326364L;

            [TomlInlineComment("$ChatBot.DiscordBridge.OwnersIds$")]
            public ulong[] OwnersIds = new[] { 978757810781323276UL };

            [TomlInlineComment("$ChatBot.DiscordBridge.MessageSendTimeout$")]
            public int Message_Send_Timeout = 3;

            [TomlPrecedingComment("$ChatBot.DiscordBridge.Formats$")]
            public string PrivateMessageFormat = "**[Private Message]** {username}: {message}";
            public string PublicMessageFormat = "{username}: {message}";
            public string TeleportRequestMessageFormat = "A new Teleport Request from **{username}**!";

            public void OnSettingUpdate()
            {
                Message_Send_Timeout = Message_Send_Timeout <= 0 ? 3 : Message_Send_Timeout;
            }
        }

        public DiscordBridge()
        {
            instance = this;
        }

        public override void Initialize()
        {
            McClient.dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CommandName)
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                )
            );

            McClient.dispatcher.Register(l => l.Literal(CommandName)
                .Then(l => l.Literal("direction")
                    .Then(l => l.Literal("both")
                        .Executes(r => OnCommandDirection(r.Source, BridgeDirection.Both)))
                    .Then(l => l.Literal("mc")
                        .Executes(r => OnCommandDirection(r.Source, BridgeDirection.Minecraft)))
                    .Then(l => l.Literal("discord")
                        .Executes(r => OnCommandDirection(r.Source, BridgeDirection.Discord)))
                )
                .Then(l => l.Literal("_help")
                    .Executes(r => OnCommandHelp(r.Source, string.Empty))
                    .Redirect(McClient.dispatcher.GetRoot().GetChild("help").GetChild(CommandName)))
            );

            Task.Run(async () => await MainAsync());
        }

        public override void OnUnload()
        {
            McClient.dispatcher.Unregister(CommandName);
            McClient.dispatcher.GetRoot().GetChild("help").RemoveChild(CommandName);
            Disconnect();
        }

        private int OnCommandHelp(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>   "dscbridge direction <both|mc|discord>"
                                   + '\n' + McClient.dispatcher.GetAllUsageString(CommandName, false),
#pragma warning restore format // @formatter:on
            });
        }

        private int OnCommandDirection(CmdResult r, BridgeDirection direction)
        {
            string bridgeName;
            switch (direction)
            {
                case BridgeDirection.Both:
                    bridgeName = Translations.bot_DiscordBridge_direction_both;
                    bridgeDirection = BridgeDirection.Both;
                    break;

                case BridgeDirection.Minecraft:
                    bridgeName = Translations.bot_DiscordBridge_direction_minecraft;
                    bridgeDirection = BridgeDirection.Minecraft;
                    break;

                case BridgeDirection.Discord:
                    bridgeName = Translations.bot_DiscordBridge_direction_discord;
                    bridgeDirection = BridgeDirection.Discord;
                    break;

                default:
                    goto case BridgeDirection.Both;
            }
            return r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.bot_DiscordBridge_direction, bridgeName));
        }

        ~DiscordBridge()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (discordBotClient != null)
            {
                try
                {
                    if (discordChannel != null)
                        discordBotClient.SendMessageAsync(discordChannel, new DiscordEmbedBuilder
                        {
                            Description = Translations.bot_DiscordBridge_disconnected,
                            Color = new DiscordColor(0xFF0000)
                        }).Wait(Config.Message_Send_Timeout * 1000);
                }
                catch (Exception e)
                {
                    LogToConsole("§§4§l§f" + Translations.bot_DiscordBridge_canceled_sending);
                    LogDebugToConsole(e);
                }

                discordBotClient.DisconnectAsync().Wait();
                IsConnected = false;
            }
        }

        public static DiscordBridge? GetInstance()
        {
            return instance;
        }

        public override void GetText(string text)
        {
            if (!CanSendMessages())
                return;

            text = GetVerbatim(text).Trim();

            // Stop the crash when an empty text is recived somehow
            if (string.IsNullOrEmpty(text))
                return;

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
            if (!CanSendMessages() || string.IsNullOrEmpty(message))
                return;

            try
            {
                discordBotClient!.SendMessageAsync(discordChannel, message).Wait(Config.Message_Send_Timeout * 1000);
            }
            catch (Exception e)
            {
                LogToConsole("§§4§l§f" + Translations.bot_DiscordBridge_canceled_sending);
                LogDebugToConsole(e);
            }
        }

        public void SendMessage(DiscordMessageBuilder builder)
        {
            if (!CanSendMessages())
                return;

            try
            {
                discordBotClient!.SendMessageAsync(discordChannel, builder).Wait(Config.Message_Send_Timeout * 1000);
            }
            catch (Exception e)
            {
                LogToConsole("§§4§l§f" + Translations.bot_DiscordBridge_canceled_sending);
                LogDebugToConsole(e);
            }
        }

        public void SendMessage(DiscordEmbedBuilder embedBuilder)
        {
            if (!CanSendMessages())
                return;

            try
            {
                discordBotClient!.SendMessageAsync(discordChannel, embedBuilder).Wait(Config.Message_Send_Timeout * 1000);
            }
            catch (Exception e)
            {
                LogToConsole("§§4§l§f" + Translations.bot_DiscordBridge_canceled_sending);
                LogDebugToConsole(e);
            }
        }
        public void SendImage(string filePath, string? text = null)
        {
            if (!CanSendMessages())
                return;

            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    filePath = filePath[(filePath.IndexOf(Path.DirectorySeparatorChar) + 1)..];
                    var messageBuilder = new DiscordMessageBuilder();

                    if (text != null)
                        messageBuilder.WithContent(text);

                    messageBuilder.WithFiles(new Dictionary<string, Stream>() { { $"attachment://{filePath}", fs } });

                    discordBotClient!.SendMessageAsync(discordChannel, messageBuilder).Wait(Config.Message_Send_Timeout * 1000);
                }
            }
            catch (Exception e)
            {
                LogToConsole("§§4§l§f" + Translations.bot_DiscordBridge_canceled_sending);
                LogDebugToConsole(e);
            }
        }

        public void SendFile(FileStream fileStream)
        {
            if (!CanSendMessages())
                return;

            SendMessage(new DiscordMessageBuilder().WithFile(fileStream));
        }

        private bool CanSendMessages()
        {
            return discordBotClient != null && discordChannel != null && bridgeDirection != BridgeDirection.Minecraft;
        }

        async Task MainAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Config.Token.Trim()))
                {
                    LogToConsole(Translations.bot_DiscordBridge_missing_token);
                    UnloadBot();
                    return;
                }

                discordBotClient = new DiscordClient(new DiscordConfiguration()
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
                    await discordBotClient.GetGuildAsync(Config.GuildId);
                }
                catch (Exception e)
                {
                    if (e is NotFoundException)
                    {
                        LogToConsole(string.Format(Translations.bot_DiscordBridge_guild_not_found, Config.GuildId));
                        UnloadBot();
                        return;
                    }

                    LogDebugToConsole("Exception when trying to find the guild:");
                    LogDebugToConsole(e);
                }

                try
                {
                    discordChannel = await discordBotClient.GetChannelAsync(Config.ChannelId);
                }
                catch (Exception e)
                {
                    if (e is NotFoundException)
                    {
                        LogToConsole(string.Format(Translations.bot_DiscordBridge_channel_not_found, Config.ChannelId));
                        UnloadBot();
                        return;
                    }

                    LogDebugToConsole("Exception when trying to find the channel:");
                    LogDebugToConsole(e);
                }

                discordBotClient.MessageCreated += async (source, e) =>
                {
                    if (e.Guild.Id != Config.GuildId)
                        return;

                    if (e.Channel.Id != Config.ChannelId)
                        return;

                    if (!Config.OwnersIds.Contains(e.Author.Id))
                        return;

                    string message = e.Message.Content.Trim();

                    if (string.IsNullOrEmpty(message) || string.IsNullOrWhiteSpace(message))
                        return;

                    if (bridgeDirection == BridgeDirection.Discord)
                    {
                        if (!message.StartsWith(".dscbridge"))
                            return;
                    }

                    if (message.StartsWith("."))
                    {
                        message = message[1..];
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromName(discordBotClient, ":gear:"));

                        CmdResult result = new();
                        PerformInternalCommand(message, ref result);

                        await e.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(discordBotClient, ":gear:"));
                        await e.Message.CreateReactionAsync(DiscordEmoji.FromName(discordBotClient, ":white_check_mark:"));
                        await e.Message.RespondAsync($"{Translations.bot_DiscordBridge_command_executed}:\n```{result}```");
                    }
                    else SendText(message);
                };

                discordBotClient.ComponentInteractionCreated += async (s, e) =>
                {
                    if (!(e.Id.Equals("accept_teleport") || e.Id.Equals("deny_teleport")))
                        return;

                    string result = e.Id.Equals("accept_teleport") ? "Accepted :white_check_mark:" : "Denied :x:";
                    SendText(e.Id.Equals("accept_teleport") ? "/tpaccept" : "/tpdeny");
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent(result));
                };

                await discordBotClient.ConnectAsync();

                await discordBotClient.SendMessageAsync(discordChannel, new DiscordEmbedBuilder
                {
                    Description = Translations.bot_DiscordBridge_connected,
                    Color = new DiscordColor(0x00FF00)
                });

                IsConnected = true;
                LogToConsole("§§2§l§f" + Translations.bot_DiscordBridge_connected);
                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                LogToConsole("§§4§l§f" + Translations.bot_DiscordBridge_unknown_error);
                LogToConsole(e);
                return;
            }
        }
    }
}
