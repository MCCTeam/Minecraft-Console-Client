//MCCScript 1.0
//using System.Threading.Tasks;
//install https://github.com/Nekiplay/Minecraft-Console-Client-Scripts/releases/download/1.0/Newtonsoft.Json.dll Newtonsoft.Json.dll
//dll Newtonsoft.Json.dll
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//==== INFO START ====
// Download Newtonsoft.Json.dll and install it into the program folder Link: https://www.newtonsoft.com/json
//==== INFO END  ====

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
    private int LastTs { get; set; }
    private int lastrand;
    private string Server { get; set; }
    private string Key { get; set; }
    private Action<string, string, string> OnMessageReceivedCallback { get; set; }
    private string BotCommunityId { get; set; }

    private void Init()
    {
        var jsonResult = CallVkMethod("groups.getLongPollServer", "group_id=" + BotCommunityId);
        var data = Json.ParseJson(jsonResult);

        Key = data.Properties["response"].Properties["key"].StringValue;
        Server = data.Properties["response"].Properties["server"].StringValue;
        LastTs = Convert.ToInt32(data.Properties["response"].Properties["ts"].StringValue);
	lastrand = LastTs + 1;
    }

    public void SendMessage(string chatId, string text, string keyboard = "", int random_id = 0)
    {
	if (random_id == 0)
	{
		random_id = lastrand;
		lastrand++;
	}
		
	CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + random_id + "&message=" + text + "&keyboard=" + keyboard);
    }
    
    public void SendMessageAndDocument(string chatId, string text, string file, string title, string keyboard = "", int random_id = 0)
    {
		if (random_id == 0)
		{
			random_id = lastrand;
			lastrand++;
		}
		var c = new WebClient();
		//
		var u = CallVkMethod("docs.getMessagesUploadServer", "peer_id=" + chatId + "&type=doc");
		var j = JsonConvert.DeserializeObject(u) as JObject;
		//
		var u2 = j["response"]["upload_url"].ToString();
		var r2 = Encoding.UTF8.GetString(c.UploadFile(u2, "POST", file));
		var j2 = JsonConvert.DeserializeObject(r2) as JObject;
		//
		var r3 = CallVkMethod("docs.save", "&file=" + j2["file"]
				+ "&title=" + title);
		var j3 = JsonConvert.DeserializeObject(r3) as JObject;
		var at = "doc"+ j3["response"]["doc"]["owner_id"].ToString() + "_" + j3["response"]["doc"]["id"].ToString();
		CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + random_id + "&message=" + text + "&keyboard=" + keyboard + "&attachment=" + at);
    }
	
    public void SendSticker(string chatId, int sticker_id, int random_id = 0)
    {
	if (random_id == 0)
	{
            random_id = lastrand;
	    lastrand++;
	}
		
	CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + random_id + "&sticker_id=" + sticker_id);
    }
    
    public void OnlineGroup(bool enable = true)
    {
	if (enable)
		CallVkMethod("groups.enableOnline", "group_id=" + BotCommunityId);
	else
		CallVkMethod("groups.disableOnline", "group_id=" + BotCommunityId);
    }
    
    public void KickChatUser(string chat_id, string user_id)
    {
	CallVkMethod("messages.removeChatUser", "chat_id=" + chat_id + "&user_id=" + user_id + "&member_id=" + user_id);
    }
	
    public class Keyboard
    {
        public bool one_time = false;
        public List<List<object>> buttons = new List<List<object>>();
        public Keyboard(bool one_time2)
        {
			one_time = one_time2;
        } 
		
	public void AddButton(string label, string payload, string color)
	{
		buttons button = new Buttons(label, payload, color);
            	buttons.Add( new List<object>() { button });
	}
		
	public class Buttons
	{
		public Action action;
		public string color;
		public Buttons(string labe11, string payload1, string color2)
		{
			action = new Action(labe11, payload1);
			color = color2;
		} 
			
		public class Action
		{
			public string type;
			public string payload;
			public string label;
			public Action(string label3, string payload3)
			{
				type = "text";
				payload = "{\"button\": \"" + payload3 + "\"}";
				label = label3;
			}
		}
	}
    }
	
    private void StartLongPoolAsync()
    {
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                var baseUrl = String.Format("{0}?act=a_check&version=2&wait=25&key={1}&ts=", Server, Key);
                
                var data = ReceiverWebClient.DownloadString(baseUrl + LastTs);
                var messages = ProcessResponse(data);

                foreach (var message in messages)
                {
                    OnMessageReceivedCallback(message.Item1, message.Item2, message.Item3);
                }
            }
        });
    }

    private IEnumerable<Tuple<string, string, string>> ProcessResponse(string jsonData)
    {
        var data = Json.ParseJson(jsonData);
	if (data.Properties.ContainsKey("failed")) // Update Key on Server Error
		Init();
        
        LastTs = Convert.ToInt32(data.Properties["ts"].StringValue);
        lastrand = LastTs + 1;
	    
        var updates = data.Properties["updates"].DataArray;
        var messages = new List<Tuple<string, string, string>>();
        foreach (var str in updates)
        {
            if (str.Properties["type"].StringValue != "message_new") continue;

            var msgData = str.Properties["object"].Properties;

            var userId = msgData["from_id"].StringValue;
	    var peer_id = msgData["peer_id"].StringValue;
            var msgText = msgData["text"].StringValue;

            messages.Add(new Tuple<string, string, string>(userId, peer_id, msgText));
        }

        return messages;
    }
    private string CallVkMethod(string methodName, string data)
    {
        var url = String.Format("https://api.vk.com/method/{0}?v=5.120&access_token={1}&{2}", methodName, Token, data);
        var jsonResult = SenderWebClient.DownloadString(url);

        return jsonResult;
    }
}
