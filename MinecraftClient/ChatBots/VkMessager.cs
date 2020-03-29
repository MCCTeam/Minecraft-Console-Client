using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Clients;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot forwarding messages between Minecraft and VKonrakte chats.
    /// Shares only messages that starts with dot ("."). Example: .Hello!
    /// Also, send message to VK when any player joins or leaves.
    /// 
    /// Needs: 
    /// - VK Community token (also LongPool API with NewMessageEvent, api >= 5.80),
    /// - VK ChatId (typically 2000000001, etc.)
    /// - Bot's CommunityId
    /// </summary>
    public class VkMessager : ChatBot
    {
        private VkLongPoolClient VkLongPoolClient { get; set; }
        private string ChatId { get; }

        /// <summary>
        /// This bot forwarding messages between Minecraft and VKonrakte chats.
        /// Shares only messages that starts with dot ("."). Example: .Hello!
        /// Also, send message to VK when any player joins or leaves.
        /// </summary>
        /// <param name="vkToken">VK Community token</param>
        /// <param name="chatId">VK ChatId</param>
        /// <param name="botCommunityId">Bot's CommunityId</param>
        public VkMessager(string vkToken, string chatId, string botCommunityId)
        {
            VkLongPoolClient = new VkLongPoolClient(vkToken, botCommunityId, ProcessMsgFromVk);
            ChatId = chatId;
        }

        public override void GetText(string text)
        {
            text = GetVerbatim(text);
            string sender = "";
            string message = "";

            if (IsChatMessage(text, ref message, ref sender))
            {
                ProcessMsgFromMinecraft(sender, message);
            }
            else if (IsPrivateMessage(text, ref message, ref sender))
            {
                ProcessMsgFromMinecraft(sender, message);
            }
            else
            {
                ProcessMsgFromMinecraft("Server", text);
            }
        }

        private void ProcessMsgFromVk(string senderId, string text)
        {
            if (!text.StartsWith(".")) return;

            SendText("[VK " + senderId.Substring(0, 2) + "]: " + text.TrimStart('.'));
        }

        private void ProcessMsgFromMinecraft(string senderName, string text)
        {
            if (!text.StartsWith(".") && !text.Contains("left") && !text.Contains("joined")) return;
            if (text.Contains("[VK")) return; // loop protection

            VkLongPoolClient.SendMessage(ChatId, $"[MC {senderName}]\r\n{text.TrimStart('.')}");
        }
    }
}
