using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftClient.Clients
{
    /// <summary>
    /// Client for VK Community (bot) LongPool API.
    /// Also can send messages.
    /// </summary>
    public class VkLongPoolClient
    {
        public VkLongPoolClient(string token, string botCommunityId, Action<string, string, string, string, string> onMessageReceivedCallback, IWebProxy webProxy = null)
        {
            Token = token;
            BotCommunityId = botCommunityId;
            OnMessageReceivedCallback = onMessageReceivedCallback;
            ReceiverWebClient = new WebClient() { Proxy = webProxy, Encoding = Encoding.UTF8 };
            SenderWebClient = new WebClient() { Proxy = webProxy, Encoding = Encoding.UTF8 };

            Init();
            StartLongPoolAsync();
        }

        private WebClient ReceiverWebClient { get; set; }
        private WebClient SenderWebClient { get; set; }
        private string Token { get; set; }
        private string LastTs { get; set; }
        private string Server { get; set; }
        private string Key { get; set; }
        private Action<string, string, string, string, string> OnMessageReceivedCallback { get; set; }
        private string BotCommunityId { get; set; }

        private void Init()
        {
            var jsonResult = CallVkMethod("groups.getLongPollServer", $"group_id={BotCommunityId}");
            var data = Json.ParseJson(jsonResult);

            Key = data.Properties["response"].Properties["key"].StringValue;
            Server = data.Properties["response"].Properties["server"].StringValue;
            LastTs = data.Properties["response"].Properties["ts"].StringValue;
        }
        public void SendMessage(string chatId, string text)
        {
            CallVkMethod("messages.send", "peer_id=" + chatId + "&message=" + text);
        }
        public string GetChat(string chatId)
        {
            return CallVkMethod("messages.getChat", "chat_id=" + chatId);
        }
        private void StartLongPoolAsync()
        {
            var baseUrl = $"{Server}?act=a_check&version=2&wait=25&key={Key}&ts=";
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var data = ReceiverWebClient.DownloadString(baseUrl + LastTs);
                    var messages = ProcessResponse(data);

                    foreach (var message in messages)
                    {
                        OnMessageReceivedCallback(message.Item1, message.Item2, message.Item3, message.Item4, message.Item5);
                    }
                }
            });
        }

        private IEnumerable<Tuple<string, string, string, string, string>> ProcessResponse(string jsonData)
        {
            var data = Json.ParseJson(jsonData);
            LastTs = data.Properties["ts"].StringValue;

            var updates = data.Properties["updates"].DataArray;
            var messages = new List<Tuple<string, string, string, string, string>>();
            foreach (var str in updates)
            {
                if (str.Properties["type"].StringValue != "message_new") continue;

                var msgData = str.Properties["object"].Properties;
                var userId = msgData["from_id"].StringValue;
                var msgText = msgData["text"].StringValue;
                var peer_id = msgData["peer_id"].StringValue;

                var actiontype = "null";
                var member_id = "null";
                try
                {
                    actiontype = str.Properties["object"].Properties["action"].Properties["type"].StringValue;
                    member_id = str.Properties["object"].Properties["action"].Properties["member_id"].StringValue;
                }
                catch 
                {
                    actiontype = "null";
                    member_id = "null";
                }

                messages.Add(new Tuple<string, string, string, string, string>(userId, msgText, peer_id, actiontype, member_id));
            }

            return messages;
        }

        private string CallVkMethod(string methodName, string data)
        {
            var url = $"https://api.vk.com/method/{methodName}?v=5.80&access_token={Token}&{data}";
            var jsonResult = SenderWebClient.DownloadString(url);

            return jsonResult;
        }
    }
}