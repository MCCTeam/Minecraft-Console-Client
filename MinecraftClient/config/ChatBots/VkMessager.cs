//MCCScript 1.0
//using System.Threading.Tasks;
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
	/* VK Client*/
	public VkLongPoolClient(string token, string botCommunityId, Action<string, string, string, string, string, string> onMessageReceivedCallback, IWebProxy webProxy = null)
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
	private string Server { get; set; }
	private string Key { get; set; }
	private Action<string, string, string, string, string, string> OnMessageReceivedCallback { get; set; }
	private string BotCommunityId { get; set; }
	private Random rnd = new Random();

	/* Utils */
	public string Utils_GetShortLink(string url)
	{
		string json = CallVkMethod("utils.getShortLink", "url=" + url);
		var j = JsonConvert.DeserializeObject(json) as JObject;
		var link = j["response"]["short_url"].ToString();
		return link;
	}
	/* Docs */
	public string Docs_GetMessagesUploadServer(string peer_id, string type, string file)
	{
		string string1 = CallVkMethod("docs.getMessagesUploadServer", "peer_id=" + peer_id + "&type=" + type);
		string uploadurl = Regex.Match(string1, "\"upload_url\":\"(.*)\"").Groups[1].Value.Replace(@"\/", "/");
		return uploadurl;
	}
	public string Docs_Upload(string url, string file)
	{
		var c = new WebClient();
		var r2 = Encoding.UTF8.GetString(c.UploadFile(url, "POST", file));
		return r2;
	}
	public string Docs_Save(string file, string title)
	{
		var j2 = JsonConvert.DeserializeObject(file) as JObject;
		return CallVkMethod("docs.save", "&file=" + j2["file"].ToString() + "&title=" + title);
	}
	public string Docs_GetSendAttachment(string file)
	{
		var j3 = JsonConvert.DeserializeObject(file) as JObject;
		var at = "doc" + j3["response"]["doc"]["owner_id"].ToString() + "_" + j3["response"]["doc"]["id"].ToString();
		return at;
	}
	/* Groups */
	public void Groups_Online(bool enable = true)
	{
		if (enable)
			CallVkMethod("groups.enableOnline", "group_id=" + BotCommunityId);
		else
			CallVkMethod("groups.disableOnline", "group_id=" + BotCommunityId);
	}
	public string Groups_GetById_GetName(string group_id)
	{
		try
		{
			string js = CallVkMethod("groups.getById", "group_id=" + group_id);
			var j3 = JsonConvert.DeserializeObject(js) as JObject;
			string name = j3["response"][0]["name"].ToString();
			return name;
		} catch { return ""; }
	}
	/* Messages */
	public void Messages_Kick_Group(string chat_id, string user_id)
	{
		if (user_id != BotCommunityId)
		CallVkMethod("messages.removeChatUser", "chat_id=" + chat_id + "&member_id=" + "-" + user_id);
	}
	public void Messages_Kick_User(string chat_id, string user_id)
	{
		CallVkMethod("messages.removeChatUser", "chat_id=" + chat_id + "&user_id=" + user_id + "&member_id=" + user_id);
	}
	public void Messages_SetActivity(string chatId, string type = "typing")
	{
		string id3 = chatId;
		id3 = id3.Substring(1);
		int ind = Convert.ToInt32(id3);
		CallVkMethod("messages.setActivity", "user_id=" + BotCommunityId + "&peer_id=" + chatId + "&group_id=" + "&type=" + type + "&group_id=" + BotCommunityId);
		CallVkMethod("messages.setActivity", "user_id=" + BotCommunityId + "&peer_id=" + ind + "&type=" + type + "&group_id=" + BotCommunityId);
	}
	public string Messages_GetInviteLink(string chatId, bool reset)
	{
		string json = "";
		if (reset == true)
			json = CallVkMethod("messages.getInviteLink", "peer_id=" + chatId + "&group_id=" + BotCommunityId + "&reset=1");
		else
			json = CallVkMethod("messages.getInviteLink", "peer_id=" + chatId + "&group_id=" + BotCommunityId);
		var j = JsonConvert.DeserializeObject(json) as JObject;
		var link = j["response"]["link"].ToString();
		return link;
	}
	/* Messages Send */
	public void Messages_Send_Text(string chatId, string text)
	{
		string reply = CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&message=" + text);
		try
		{
		var j = JsonConvert.DeserializeObject(reply) as JObject;
		if (j["error"]["error_code"].ToString() != "")
			CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&message=" + text);
		} catch { }
	}
	public void Messages_Send_Keyboard(string chatId, Keyboard keyboard)
	{
		string kb = keyboard.GetKeyboard();
		string reply = CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&keyboard=" + kb);
		try
		{
		var j = JsonConvert.DeserializeObject(reply) as JObject;
		if (j["error"]["error_code"].ToString() != "")
			CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&keyboard=" + kb);
		} catch { }
	}
	public void Messages_Send_TextAndKeyboard(string chatId, string text, Keyboard keyboard)
	{
		string kb = keyboard.GetKeyboard();
		string reply = CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&message=" + text + "&keyboard=" + kb);
		try
		{
		var j = JsonConvert.DeserializeObject(reply) as JObject;
		if (j["error"]["error_code"].ToString() != "")
			CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&message=" + text + "&keyboard=" + kb);
		} catch { }
	}
	public void Messages_Send_Sticker(string chatId, int sticker_id)
	{
		string reply = CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&sticker_id=" + sticker_id);
		try
		{
		var j = JsonConvert.DeserializeObject(reply) as JObject;
		if (j["error"]["error_code"].ToString() != "")
			CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&sticker_id=" + sticker_id);
		} catch { }
	}
	public void Messages_Send_TextAndDocument(string chatId, string text, string file, string title)
	{
		string u2 = Docs_GetMessagesUploadServer(chatId, "doc", file);
		string r2 = Docs_Upload(u2, file);
		string r3 = Docs_Save(r2, title);
		string at = Docs_GetSendAttachment(r3);
		string reply = CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&message=" + text + "&attachment=" + at);
		try
		{
		var j = JsonConvert.DeserializeObject(reply) as JObject;
			if (j["error"]["error_code"].ToString() != "")
				CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + "&message=" + text + "&attachment=" + at);
		} catch { }
	}
	public void Messages_Send_Custom(string chatId, string custom)
	{
		string reply = CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + custom);
		try
		{
		var j = JsonConvert.DeserializeObject(reply) as JObject;
		if (j["error"]["error_code"].ToString() != "")
			CallVkMethod("messages.send", "peer_id=" + chatId + "&random_id=" + rnd.Next() + custom);
		} catch { }
	}	
	/* Messages GetConversationMembers*/
	public int Messages_GetConversationMembers_GetCount_v1(string chatId)
	{
		var json = CallVkMethod("messages.getConversationMembers", "peer_id=" + chatId + "&group_id=" + BotCommunityId);
		var j = JsonConvert.DeserializeObject(json) as JObject;
		int u2 = int.Parse(j["response"]["count"].ToString());
		return u2;
	}
	public string Messages_GetConversationMembers_GetProfiles(string chatId)
	{
		var json = CallVkMethod("messages.getConversationMembers", "peer_id=" + chatId + "&group_id=" + BotCommunityId);
		string ids = "";
		JObject json1 = JObject.Parse(json);
		IList<JToken> results = json1["response"]["profiles"].Children().ToList();
		foreach (JToken result in results)
		{
			string id = result["id"].ToString();
			if (!id.Contains("-"))
				ids += id + ", ";
		}
		return ids;
	}
	public string Messages_GetConversationMembers_GetItems_member_id(string chatId)
	{
		var json = CallVkMethod("messages.getConversationMembers", "peer_id=" + chatId + "&group_id=" + BotCommunityId);
		string ids = "";
		JObject json1 = JObject.Parse(json);
		IList<JToken> results = json1["response"]["items"].Children().ToList();
		foreach (JToken result in results)
		{
			string id = result["member_id"].ToString();
			if (!id.Contains("-"))
				ids += id + ", ";
		}
		return ids;
	}

	public class Keyboard
	{
		public bool one_time = false;
		public List<List<object>> buttons = new List<List<object>>();
		public bool inline = false;
		public Keyboard(bool one_time2, bool line = false)
		{
			if (line == true && one_time2 == true)
				one_time2 = false;

			one_time = one_time2;
			inline = line;
		}

		public void AddButton(string label, string payload, string color)
		{
			Buttons button = new Buttons(label, payload, color);
			buttons.Add(new List<object>() { button });
		}
		public string GetKeyboard()
        {
			return JsonConvert.SerializeObject(this, Formatting.Indented); ;
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
	
	/* LoongPool */
	private void Init()
	{
		var jsonResult = CallVkMethod("groups.getLongPollServer", "group_id=" + BotCommunityId);
		var data = Json.ParseJson(jsonResult);

		Key = data.Properties["response"].Properties["key"].StringValue;
		Server = data.Properties["response"].Properties["server"].StringValue;
		LastTs = Convert.ToInt32(data.Properties["response"].Properties["ts"].StringValue);
	}
	private void StartLongPoolAsync()
	{
		Task.Factory.StartNew(() =>
		{
			while (true)
			{
				try
				{
					string baseUrl = String.Format("{0}?act=a_check&version=2&wait=25&key={1}&ts=", Server, Key);
					var data = ReceiverWebClient.DownloadString(baseUrl + LastTs);
					var messages = ProcessResponse(data);

					foreach (var message in messages)
					{
						OnMessageReceivedCallback(message.Item1, message.Item2, message.Item3, message.Item4, message.Item5, message.Item6);
					}

				}
				catch { }
			}
		});
	}

	private IEnumerable<Tuple<string, string, string, string, string, string>> ProcessResponse(string jsonData)
	{
		var j = JsonConvert.DeserializeObject(jsonData) as JObject;
		var data = Json.ParseJson(jsonData);
		if (data.Properties.ContainsKey("failed"))
		{
			Init();
		}
		LastTs = Convert.ToInt32(data.Properties["ts"].StringValue);
		var updates = data.Properties["updates"].DataArray;
		var messages = new List<Tuple<string, string, string, string, string, string>>();
		foreach (var str in updates)
		{
			if (str.Properties["type"].StringValue != "message_new") continue;

			var msgData = str.Properties["object"].Properties;

			var id = msgData["from_id"].StringValue;
			var userId = msgData["from_id"].StringValue;
			var peer_id = msgData["peer_id"].StringValue;
			string event_id = "";
			var msgText = msgData["text"].StringValue;
			var conversation_message_id = msgData["conversation_message_id"].StringValue;

			messages.Add(new Tuple<string, string, string, string, string, string>(userId, peer_id, msgText, conversation_message_id, id, event_id));
		}

		return messages;
	}

	public string CallVkMethod(string methodName, string data)
	{
		try
		{
			var url = String.Format("https://api.vk.com/method/{0}?v=5.122&access_token={1}&{2}", methodName, Token, data);
			var jsonResult = SenderWebClient.DownloadString(url);

			return jsonResult;
		}
		catch { return String.Empty; }
	}
}
