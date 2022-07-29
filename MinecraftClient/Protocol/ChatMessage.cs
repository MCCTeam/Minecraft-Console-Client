using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol
{
    public class ChatMessage
    {
        // in 1.19 and above,  isSignedChat = true
        public readonly bool isSignedChat;

        public readonly string content;

        public readonly bool isJson;

        //  0: chat (chat box), 1: system message (chat box), 2: game info (above hotbar), 3: say command,
        //  4: msg command,     5: team msg command,          6: emote command,            7: tellraw command
        public readonly int chatType;

        public readonly Guid senderUUID;

        public readonly bool isSystemChat;

        public readonly string? unsignedContent;

        public readonly string? displayName;

        public readonly string? teamName;

        public readonly DateTime? timestamp;

        public readonly bool? isSignatureLegal;

        public ChatMessage(string content, bool isJson, int chatType, Guid senderUUID, string? unsignedContent, string displayName, string? teamName, long timestamp, bool isSignatureLegal)
        {
            this.isSignedChat = true;
            this.isSystemChat = false;
            this.content = content;
            this.isJson = isJson;
            this.chatType = chatType;
            this.senderUUID = senderUUID;
            this.unsignedContent = unsignedContent;
            this.displayName = displayName;
            this.teamName = teamName;
            this.timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime; 
            this.isSignatureLegal = isSignatureLegal;
        }

        public ChatMessage(string content, bool isJson, int chatType, Guid senderUUID, bool isSystemChat = false)
        {
            this.isSignedChat = false;
            this.content = content;
            this.isJson = isJson;
            this.chatType = chatType;
            this.senderUUID = senderUUID;
            this.isSystemChat = isSystemChat;
        }
    }
}
