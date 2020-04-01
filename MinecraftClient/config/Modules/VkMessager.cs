//MCCScript 1.0
//using System.Threading.Tasks;

//==== CONFIG START ====
string vkToken = "";
string chatId = "";
string botCommunityId = "";
//====  CONFIG END  ====

MCC.LoadBot(new VkMessager(vkToken, chatId, botCommunityId));

//MCCScript Extensions

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
    private readonly string ChatId;

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

        VkLongPoolClient.SendMessage(ChatId, String.Format("[MC {0}]\r\n{1}", senderName, text.TrimStart('.')));
    }
}

/// <summary>
/// Client for VK Community (bot) LongPool API.
/// Also can send messages.
/// </summary>
internal class VkLongPoolClient
{
    public VkLongPoolClient(string token, string botCommunityId, Action<string, string> onMessageReceivedCallback, IWebProxy webProxy = null)
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
    private Action<string, string> OnMessageReceivedCallback { get; set; }
    private string BotCommunityId { get; set; }

    private void Init()
    {
        var jsonResult = CallVkMethod("groups.getLongPollServer", "group_id=" + BotCommunityId);
        var data = Json.ParseJson(jsonResult);

        Key = data.Properties["response"].Properties["key"].StringValue;
        Server = data.Properties["response"].Properties["server"].StringValue;
        LastTs = data.Properties["response"].Properties["ts"].StringValue;
    }

    public void SendMessage(string chatId, string text)
    {
        CallVkMethod("messages.send", "peer_id=" + chatId + "&message=" + text);
    }

    private void StartLongPoolAsync()
    {
        var baseUrl = String.Format("{0}?act=a_check&version=2&wait=25&key={1}&ts=", Server, Key);
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                var data = ReceiverWebClient.DownloadString(baseUrl + LastTs);
                var messages = ProcessResponse(data);

                foreach (var message in messages)
                {
                    OnMessageReceivedCallback(message.Item1, message.Item2);
                }
            }
        });
    }

    private IEnumerable<Tuple<string, string>> ProcessResponse(string jsonData)
    {
        var data = Json.ParseJson(jsonData);
        LastTs = data.Properties["ts"].StringValue;

        var updates = data.Properties["updates"].DataArray;
        var messages = new List<Tuple<string, string>>();
        foreach (var str in updates)
        {
            if (str.Properties["type"].StringValue != "message_new") continue;

            var msgData = str.Properties["object"].Properties;

            var userId = msgData["from_id"].StringValue;
            var msgText = msgData["text"].StringValue;

            messages.Add(new Tuple<string, string>(userId, msgText));
        }

        return messages;
    }

    private string CallVkMethod(string methodName, string data)
    {
        var url = String.Format("https://api.vk.com/method/{0}?v=5.80&access_token={1}&{2}", methodName, Token, data);
        var jsonResult = SenderWebClient.DownloadString(url);

        return jsonResult;
    }
}