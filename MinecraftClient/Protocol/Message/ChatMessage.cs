using System;

namespace MinecraftClient.Protocol.Message
{
    public class ChatMessage
    {
        // in 1.19 and above,  isSignedChat = true
        public readonly bool isSignedChat;

        public readonly string content;

        public readonly bool isJson;

        //  0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar), 3: say command,
        //  4: msg command,     5: team msg command,          6: emote command,            7: tellraw command
        public readonly int chatTypeId;

        public readonly Guid senderUUID;

        public readonly bool isSystemChat;

        public readonly string? unsignedContent;

        public readonly string? displayName;

        public readonly string? teamName;

        public readonly DateTime? timestamp;

        public readonly byte[]? signature;

        public readonly bool? isSignatureLegal;

        public ChatMessage(string content, bool isJson, int chatType, Guid senderUUID, string? unsignedContent, string displayName, string? teamName, long timestamp, byte[] signature, bool isSignatureLegal)
        {
            isSignedChat = true;
            isSystemChat = false;
            this.content = content;
            this.isJson = isJson;
            chatTypeId = chatType;
            this.senderUUID = senderUUID;
            this.unsignedContent = unsignedContent;
            this.displayName = displayName;
            this.teamName = teamName;
            this.timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            this.signature = signature;
            this.isSignatureLegal = isSignatureLegal;
        }

        public ChatMessage(string content, bool isJson, int chatType, Guid senderUUID, bool isSystemChat = false)
        {
            isSignedChat = isSystemChat;
            this.isSystemChat = isSystemChat;
            this.content = content;
            this.isJson = isJson;
            chatTypeId = chatType;
            this.senderUUID = senderUUID;
        }

        public LastSeenMessageList.Entry? ToLastSeenMessageEntry()
        {
            return signature != null ? new LastSeenMessageList.Entry(senderUUID, signature) : null;
        }

        public bool LacksSender()
        {
            return senderUUID == Guid.Empty;
        }
    }
}
