using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Tomlet.Attributes;
using File = System.IO.File;

namespace MinecraftClient.ChatBots
{
    public class TelegramBridge : ChatBot
    {
        private enum BridgeDirection
        {
            Both = 0,
            Minecraft,
            Telegram
        }

        private static TelegramBridge? instance = null;
        public bool IsConnected { get; private set; }

        private TelegramBotClient? botClient;
        private CancellationTokenSource? cancellationToken;
        private BridgeDirection bridgeDirection = BridgeDirection.Both;

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "TelegramBridge";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.TelegramBridge.Token$")]
            public string Token = "your bot token here";

            [TomlInlineComment("$ChatBot.TelegramBridge.ChannelId$")]
            public string ChannelId = "";

            [TomlInlineComment("$ChatBot.TelegramBridge.Authorized_Chat_Ids$")]
            public long[] Authorized_Chat_Ids = Array.Empty<long>();

            [TomlInlineComment("$ChatBot.TelegramBridge.MessageSendTimeout$")]
            public int Message_Send_Timeout = 3;

            [TomlPrecedingComment("$ChatBot.TelegramBridge.Formats$")]
            public string PrivateMessageFormat = "*(Private Message)* {username}: {message}";
            public string PublicMessageFormat = "{username}: {message}";
            public string TeleportRequestMessageFormat = "A new Teleport Request from **{username}**!";

            public void OnSettingUpdate()
            {
                Message_Send_Timeout = Message_Send_Timeout <= 0 ? 3 : Message_Send_Timeout;
            }
        }

        public TelegramBridge()
        {
            instance = this;
        }

        public override void Initialize()
        {
            RegisterChatBotCommand("tgbridge", "bot.TelegramBridge.desc", "tgbridge direction <both|mc|telegram>", OnTgCommand);

            Task.Run(async () => await MainAsync());
        }

        ~TelegramBridge()
        {
            Disconnect();
        }

        public override void OnUnload()
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (botClient != null)
            {
                try
                {
                    SendMessage(Translations.bot_TelegramBridge_disconnected);
                    cancellationToken?.Cancel();
                    botClient = null;
                }
                catch (Exception e)
                {
                    LogToConsole("§w§l§f" + Translations.bot_TelegramBridge_canceled_sending);
                    LogDebugToConsole(e);
                }

                IsConnected = false;
            }
        }

        public static TelegramBridge? GetInstance()
        {
            return instance;
        }

        private string OnTgCommand(string cmd, string[] args)
        {
            if (args.Length == 2)
            {
                if (args[0].ToLower().Equals("direction"))
                {
                    string direction = args[1].ToLower().Trim();

                    string bridgeName;
                    switch (direction)
                    {
                        case "b":
                        case "both":
                            bridgeName = Translations.bot_TelegramBridge_direction_both;
                            bridgeDirection = BridgeDirection.Both;
                            break;

                        case "mc":
                        case "minecraft":
                            bridgeName = Translations.bot_TelegramBridge_direction_minecraft;
                            bridgeDirection = BridgeDirection.Minecraft;
                            break;

                        case "t":
                        case "tg":
                        case "telegram":
                            bridgeName = Translations.bot_TelegramBridge_direction_Telegram;
                            bridgeDirection = BridgeDirection.Telegram;
                            break;

                        default:
                            return Translations.bot_TelegramBridge_invalid_direction;
                    }

                    return string.Format(Translations.bot_TelegramBridge_direction, bridgeName);
                };
            }

            return "dscbridge direction <both|mc|discord>";
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

            if (IsPrivateMessage(text, ref message, ref username))
                message = Config.PrivateMessageFormat.Replace("{username}", username).Replace("{message}", message).Replace("{timestamp}", GetTimestamp()).Trim();
            else if (IsChatMessage(text, ref message, ref username))
                message = Config.PublicMessageFormat.Replace("{username}", username).Replace("{message}", message).Replace("{timestamp}", GetTimestamp()).Trim();
            else if (IsTeleportRequest(text, ref username))
                message = Config.TeleportRequestMessageFormat.Replace("{username}", username).Replace("{timestamp}", GetTimestamp()).Trim();

            else message = text;

            SendMessage(message);
        }

        public void SendMessage(string message)
        {
            if (!CanSendMessages() || string.IsNullOrEmpty(message))
                return;

            try
            {
                botClient!.SendTextMessageAsync(Config.ChannelId.Trim(), message, ParseMode.Markdown).Wait(Config.Message_Send_Timeout);
            }
            catch (Exception e)
            {
                LogToConsole("§w§l§f" + Translations.bot_TelegramBridge_canceled_sending);
                LogDebugToConsole(e);
            }
        }

        public void SendImage(string filePath, string? text = null)
        {
            if (!CanSendMessages())
                return;

            try
            {
                string fileName = filePath[(filePath.IndexOf(Path.DirectorySeparatorChar) + 1)..];

                Stream stream = File.OpenRead(filePath);
                botClient!.SendDocumentAsync(
                    Config.ChannelId.Trim(),
                    document: new InputOnlineFile(content: stream, fileName),
                    caption: text,
                    parseMode: ParseMode.Markdown).Wait(Config.Message_Send_Timeout * 1000);
            }
            catch (Exception e)
            {
                LogToConsole("§w§l§f" + Translations.bot_TelegramBridge_canceled_sending);
                LogDebugToConsole(e);
            }
        }

        private bool CanSendMessages()
        {
            return botClient != null && !string.IsNullOrEmpty(Config.ChannelId.Trim()) && bridgeDirection != BridgeDirection.Minecraft;
        }

        async Task MainAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(Config.Token.Trim()))
                {
                    LogToConsole(Translations.bot_TelegramBridge_missing_token);
                    UnloadBot();
                    return;
                }

                if (string.IsNullOrEmpty(Config.ChannelId.Trim()))
                    LogToConsole("§w§l§f" + Translations.bot_TelegramBridge_missing_channel_id);

                botClient = new TelegramBotClient(Config.Token.Trim());
                cancellationToken = new CancellationTokenSource();

                botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandlePollingErrorAsync,
                    receiverOptions: new ReceiverOptions
                    {
                        // receive all update types
                        AllowedUpdates = Array.Empty<UpdateType>()
                    },
                    cancellationToken: cancellationToken.Token
                );

                IsConnected = true;

                SendMessage($"✅ {Translations.bot_TelegramBridge_connected}");
                LogToConsole($"§y§l§f{Translations.bot_TelegramBridge_connected}");

                if (Config.Authorized_Chat_Ids.Length == 0)
                {
                    SendMessage($"⚠️ *{Translations.bot_TelegramBridge_missing_authorized_channels}* ⚠️");
                    LogToConsole($"§w§l§f{Translations.bot_TelegramBridge_missing_authorized_channels}");
                    return;
                }

                await Task.Delay(-1);
            }
            catch (Exception e)
            {
                LogToConsole($"§w§l§f{Translations.bot_TelegramBridge_unknown_error}");
                LogToConsole(e);
                return;
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken _cancellationToken)
        {
            // Only process Message updates: https://core.telegram.org/bots/api#message
            if (update.Message is not { } message)
                return;

            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            var text = message.Text;

            if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                return;

            if (text.ToLower().Contains("/start"))
                return;

            if (text.ToLower().Contains(".chatid"))
            {
                await botClient.SendTextMessageAsync(chatId: chatId,
                    replyToMessageId: message.MessageId,
                    text: $"Chat ID: {chatId}",
                    cancellationToken: _cancellationToken,
                    parseMode: ParseMode.Markdown);
                return;
            }

            if (Config.Authorized_Chat_Ids.Length > 0 && !Config.Authorized_Chat_Ids.Contains(chatId))
            {
                LogDebugToConsole($"Unauthorized message '{messageText}' received in a chat with with an ID: {chatId} !");
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyToMessageId: message.MessageId,
                    text: Translations.bot_TelegramBridge_unauthorized,
                    cancellationToken: _cancellationToken,
                    parseMode: ParseMode.Markdown);
                return;
            }

            LogDebugToConsole($"Received a '{messageText}' message in a chat with with an ID: {chatId} .");

            if (bridgeDirection == BridgeDirection.Telegram)
            {
                if (!text.StartsWith(".dscbridge"))
                    return;
            }

            if (text.StartsWith("."))
            {
                var command = text[1..];

                string? result = "";
                PerformInternalCommand(command, ref result);
                result = string.IsNullOrEmpty(result) ? "-" : result;

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    replyToMessageId:
                    message.MessageId,
                    text: $"{Translations.bot_TelegramBridge_command_executed}:\n\n{result}",
                    cancellationToken: _cancellationToken,
                    parseMode: ParseMode.Markdown);
            }
            else SendText(text);
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken _cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            LogToConsole("§w§l§f" + ErrorMessage);
            return Task.CompletedTask;
        }
    }
}
