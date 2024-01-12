using System;
using System.Net;
using System.Text;
using MinecraftClient.Scripting;
using WebSocketSharp;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using Tomlet.Attributes;
using MinecraftClient.Mapping;
using static MinecraftClient.Settings;
using System.Threading.Tasks;

namespace MinecraftClient.OkBots;
public class OkWsBot : ChatBot
{
    WebSocket? pyWebSocketServer;
	protected static OkWsBot? chatBot;
	
	public static Configs Config = new ();

	[TomlDoNotInlineObject]
	public class Configs {
        public bool Enabled = true;
		
        [TomlInlineComment("Python Websocket接口")]
		public string pythonSendWsApi = "ws://127.0.0.1:12345";
        [TomlInlineComment("在群内显示的服务器名称")]
		public string serverName = "1.20";
        [TomlInlineComment("在群内昵称")]
		public string groupCard = "QQbot";
	}

    public override void Initialize()
	{
		chatBot = this;
		InitDeathMsgs();

        pyWebSocketServer = new WebSocket (Config.pythonSendWsApi);
		pyWebSocketServer.OnMessage += (sender, e) => {
			var msg = e.Data;
			Console.WriteLine("We get msg:" + msg);
			if (msg == "#!list") {
				pyWebSocketServer.Send("#!list" + GetPlayerListMsg());
				return;
			}
			SendToConsole(msg);
		};
        pyWebSocketServer.Connect ();

		LogToConsole("OkQQbot has been initialized!");
	}

	public override void GetText(string text)
	{
		string message = "";
		string username = "";
		string serverPrefix = "["+ Config.serverName +"] ";
		text = GetVerbatim(text);

		if (IsChatMessage(text, ref message, ref username))
		{
			if (username == GetUsername()) return;
			string msg = serverPrefix + text;
			SendToQQ(msg);
			return;
		}

		if (IsAchievementMsg(text)) {
			if (text.StartsWith("bot_")) return;
			string msg = serverPrefix + "<喜报> " + text;
			SendToQQ(msg);
			return;
		}

		if (IsDeathMsg(text)) {
			if (text.StartsWith("bot_")) return;
			string msg = serverPrefix + "<悲报> " + text;
			SendToQQ(msg);
			return;
		}

		if (IsLoginLogoutMsg(text)) {
			if (text.StartsWith("bot_")) return;
			SendToQQ(serverPrefix + text);
		}
	}

    public void SendToConsole(string msg) {
		base.SendText(msg);
	}

	public void SendToQQ(string msg) {
		if (!pyWebSocketServer!.Ping()) {
			Console.WriteLine("Onebot Connection down, reconnecting...");
			pyWebSocketServer = new WebSocket (Config.pythonSendWsApi);
        	pyWebSocketServer.Connect ();
		}
        pyWebSocketServer.Send (msg);
	}

	public string GetPlayerListMsg() {
		string result = "["+Config.serverName+"]";
		List<string> playerStrs = new List<string>(GetOnlinePlayers());
		playerStrs.Remove(Config.groupCard);
		if (playerStrs.Count == 0) {
			return result += " [鬼服]\n没有玩家在线";
		}
		result += " 在线玩家：";
		foreach (var playerStr in playerStrs)
		{
			result += "\n" + playerStr;
		}
		return result;
	}

	public bool IsLoginLogoutMsg(string msg) {
		return msg.EndsWith(" joined the game") ||
			msg.EndsWith(" left the game");
	}

	public bool IsAchievementMsg(string msg) {
		return msg.IndexOf(" has made the advancement ") != -1;
	}

    private static List<string> DeathMsgs = new();
	public void InitDeathMsgs() {
		string translationsStr = Encoding.ASCII.GetString((byte[])MinecraftAssets.ResourceManager.GetObject("en_us.json")!);
		JObject translations = JObject.Parse(translationsStr);
		var enumerator = translations.GetEnumerator();
		while (enumerator.MoveNext()) {
			if (!enumerator.Current.Key.StartsWith("death.")) continue;
			string result = enumerator.Current.Value!.ToString();
			result = result.Replace("%1$s ", "");

			var index2s = result.IndexOf("%2$s");
			if (index2s != -1) {
				result = result.Substring(0,index2s);
			}
			DeathMsgs.Add(result);
		}
	}
	public bool IsDeathMsg(string msg) {
		foreach (var item in DeathMsgs)
		{
			if (msg.IndexOf(item) != -1) {
				return true;
			}
 		}
		return false;
	}
}