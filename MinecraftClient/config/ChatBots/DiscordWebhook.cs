//MCCScript 1.0
//using System.Collections.Specialized;

MCC.LoadBot(new DiscordWebhook());

//MCCScript Extensions
/// <summary>
/// Stores all settings for the script.
/// </summary>
class WebhoookSettings
{
    #region All variables for the main class
    public string WebhookURL { get; set; }
    public int SecondsToSaveInCache { get; set; }
    public bool SendPrivateMsg { get; set; }
    public bool CustomChatDetection { get; set; }
    public bool SendServerMsg { get; set; }
    public bool GetUUIDDirectlyFromMojang { get; set; }
    public bool Togglesending { get; set; }
    public bool AllowMentions { get; set; }
    public bool NormalChatDetection { get; set; }
    private Dictionary<string, List<string>> messageCache = new Dictionary<string, List<string>>();
    private Dictionary<string, string> messageContains = new Dictionary<string, string>();
    private Dictionary<string, string> messageFrom = new Dictionary<string, string>();
    private Dictionary<string, string> namesToUuidMojangCache = new Dictionary<string, string>();
    private List<string> ignoredPlayers = new List<string>();
    #endregion

    #region All variables for the API class
    private string currentSkinMode;
    private int size;
    private int scale;
    private bool overlay;
    private bool checkUUID;
    private string fallbackSkin;

    private Dictionary<string, string> skinModes = new Dictionary<string, string>();
    public string CurrentSkinMode
    {
        set { if (skinModes.ContainsKey(value)) { currentSkinMode = value; } }
        get { return currentSkinMode; }
    }
    public int Size
    {
        set { if (value <= 512) { size = value; } }
        get { return size; }
    }
    public int Scale
    {
        set { if (value <= 10) { this.scale = value; } }
        get { return scale; }
    }
    public bool Overlay
    {
        set { overlay = value; }
        get { return overlay; }
    }
    public bool CheckUUID
    {
        set { checkUUID = value; }
        get { return checkUUID; }
    }
    public string FallbackSkin
    {
        set { if (value == "MHF_Steve" || value == "MHF_Alex") { fallbackSkin = value; } }
        get { return fallbackSkin; }
    }
    #endregion

    /// <summary>
    /// Setup standard settings.
    /// </summary>
    public WebhoookSettings()
    {
        // Set preconfigured skinModes
        skinModes.Add("flatFace", "https://crafatar.com/avatars/{0}");
        skinModes.Add("cubeHead", "https://crafatar.com/renders/head/{0}");
        skinModes.Add("fullSkin", "https://crafatar.com/renders/body/{0}");

        // Define standard values for main class
        WebhookURL = "https://discord.com/api/webhooks/SomeNumber/RandomStuff";                // Enter your Webhook URL here to start the bot with it predefined.
        SendPrivateMsg = true;
        CustomChatDetection = true;
        SendServerMsg = true;
        GetUUIDDirectlyFromMojang = false;
        NormalChatDetection = true;
        Togglesending = true;
        checkUUID = true;
        AllowMentions = false;
        currentSkinMode = "flatFace";
        SecondsToSaveInCache = 10;

        // Define standard values for API class
        size = 100;
        scale = 4;
        overlay = true;
        fallbackSkin = "MHF_Steve";
    }

    public Dictionary<string, string> GetMessageContains() { return this.messageContains; }
    public Dictionary<string, string> GetMessageFrom() { return this.messageFrom; }
    public Dictionary<string, List<string>> GetCachedMessages() { return this.messageCache; }
    public Dictionary<string, string> GetSkinModes() { return this.skinModes; }
    public Dictionary<string, string> GetNamesToUuidMojangCache() { return this.namesToUuidMojangCache; }
    public void resetUUIDCache() { namesToUuidMojangCache.Clear(); }
    public List<string> GetIgnoredPlayers() { return ignoredPlayers; }
}

class SkinAPI
{
    WebhoookSettings settings = new WebhoookSettings();

    public SkinAPI(WebhoookSettings s)
    {
        settings = s;
    }

    /// <summary>
    /// Sends a request with the minecraft name to the mojang servers to optain its UUID.
    /// Fails if there are invalid names, due to titles getting in it.
    /// </summary>
    /// <param name="name"> Minecraft IGN </param>
    /// <returns></returns>
    public string GetUUIDFromMojang(string name)
    {
        if (settings.GetNamesToUuidMojangCache().ContainsKey(name))
            return settings.GetNamesToUuidMojangCache()[name];

        using (WebClient wc = new WebClient())
        {
            try
            {
                string uuid = Json.ParseJson(wc.DownloadString("https://api.mojang.com/users/profiles/minecraft/" + name)).Properties["id"].StringValue;
                settings.GetNamesToUuidMojangCache().Add(name, uuid);
                return uuid;
            }
            catch (Exception) { return "00000000000000000000000000000000"; }
        }
    }

    /// <summary>
    /// Gets the UUID from the internal bot command, which is faster and should be safer on servers with titles.
    /// Fails on cracked servers.
    /// </summary>
    /// <param name="name"> Minecraft IGN </param>
    /// <param name="playerList"> Dictionary of UUID's matched with the playername. </param>
    /// <returns></returns>
    public string GetUUIDFromPlayerList(string name, Dictionary<string, string> playerList)
    {
        foreach (KeyValuePair<string, string> player in playerList)
        {
            if (name.ToLower().Contains(player.Value.ToLower()))
            {
                return player.Key.Replace("-", "");
            }
        }

        // Falback if player leaves the server.
        return GetUUIDFromMojang(name);
    }

    /// <summary>
    /// Creates the url which is forewarded to the webhook.
    /// </summary>
    /// <param name="UUID"> Player UUID </param>
    /// <returns></returns>
    public string GetSkinURLCrafatar(string UUID)
    {
        string parameters = string.Join("&", "size=" + settings.Size, "scale=" + settings.Scale, "default=" + settings.FallbackSkin, (settings.Overlay ? "overlay" : ""));
        return string.Format(settings.GetSkinModes()[settings.CurrentSkinMode], UUID + "?" + parameters);
    }
}

/// <summary>
/// Stores messages in a uniform format.
/// </summary>
class Message
{
    private string senderName;
    private string senderUUID;
    private string content;
    private DateTime time;

    /// <summary>
    /// Initialize a Message.
    /// </summary>
    /// <param name="sN"> Player IGN </param>
    /// <param name="sU"> Player UUID </param>
    /// <param name="c"> Message content </param>
    /// <param name="t"></param>
    public Message(string sN, string sU, string c, DateTime t)
    {
        senderName = sN;
        senderUUID = sU;
        content = c;
        time = t;
    }

    public string SenderName
    {
        get { return senderName; }
    }
    public string SenderUUID
    {
        get { return senderUUID; }
    }
    public DateTime Time
    {
        get { return time; }
    }
    public string Content
    {
        get { return content; }
        set { content = value; }
    }
}

/// <summary>
/// Caches messages, until it is emptied by update(), or
/// a message from another user is entered.
/// </summary>
class MessageCache
{
    private WebhoookSettings settings;
    private Message msg;
    public Message Msg { get { return msg; } }

    public MessageCache(WebhoookSettings s)
    {
        msg = null;
        settings = s;
    }

    /// <summary>
    /// Add a Message to cache.
    /// Current message gets appended, if the new message is from the same sender.
    /// Otherwise the current message is given back and the new message is saved.
    /// </summary>
    /// <param name ="newMsg"> New message to add. </param>
    /// <returns>Current saved message, if the new message is from another player.</returns>
    public Message Add(Message newMsg)
    {
        if (msg == null)
        {
            msg = newMsg;
            return null;
        }
        else
        {
            if (((msg.SenderUUID == newMsg.SenderUUID && settings.CheckUUID)
                || (msg.SenderName == newMsg.SenderName && !settings.CheckUUID))
                && msg.Content.Length + newMsg.Content.Length <= 2000)
            {
                msg.Content += "\n" + newMsg.Content;
                return null;
            }
            else
            {
                Message temp = msg;
                msg = newMsg;
                return temp;
            }
        }
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    /// <returns>Current message</returns>
    public Message Clear()
    {
        Message temp = msg;
        msg = null;
        return temp;
    }
}

class HTTP
{
    public static byte[] Post(string url, NameValueCollection pairs)
    {
        using (WebClient webClient = new WebClient())
        {
            return webClient.UploadValues(url, pairs);
        }
    }
}

class DiscordWebhook : ChatBot
{
    private WebhoookSettings settings = new WebhoookSettings();
    private SkinAPI sAPI;
    private MessageCache cache;

    public DiscordWebhook()
    {
        sAPI = new SkinAPI(settings);
        cache = new MessageCache(settings);
    }

    public override void Initialize()
    {
        LogToConsole("Made by Daenges.\nSpecial thanks to Crafatar for providing the beautiful avatars!");
        LogToConsole("Please set a Webhook with '/dw changeurl [URL]'. For further information type '/discordwebhook help'.");
        RegisterChatBotCommand("discordWebhook", "/DiscordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype'", GetHelp(), CommandHandler);
        RegisterChatBotCommand("dw", "/DiscordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype'", GetHelp(), CommandHandler);
    }

    public override void Update()
    {
        if (cache.Msg != null && (DateTime.Now - cache.Msg.Time).TotalSeconds >= settings.SecondsToSaveInCache)
        {
            SendWebhook(cache.Clear());
            LogDebugToConsole("Cleared cache!");
        }
    }

    public override void OnPlayerJoin(Guid uuid, string name)
    {
        if (uuid.ToString() != string.Empty && name != string.Empty &&
            uuid.ToString() != null && name != null)
        {
            if (settings.GetNamesToUuidMojangCache().ContainsKey(name))
            {
                settings.GetNamesToUuidMojangCache().Remove(name);
            }
        }
        else
        {
            LogDebugToConsole(string.Format("Invalid player joined the game! UUID: {0}, Playername: {1}", uuid.ToString(), name));
        }
    }

    public override void OnPlayerLeave(Guid uuid, string name)
    {
        if (uuid.ToString() != string.Empty && name != string.Empty &&
            uuid.ToString() != null && name != null)
        {
            if (!settings.GetNamesToUuidMojangCache().ContainsKey(name))
            {
                settings.GetNamesToUuidMojangCache().Add(name, uuid.ToString());
            }
        }
        else
        {
            LogDebugToConsole(string.Format("Invalid player left the game! UUID: {0}, Playername: {1}", uuid.ToString(), name));
        }
    }

    public override bool OnDisconnect(DisconnectReason reason, string message)
    {
        settings.resetUUIDCache();
        return false;
    }

    public override void GetText(string text)
    {
        if (settings.Togglesending)
        {
            string message = "";
            string username = "";
            text = settings.AllowMentions ? GetVerbatim(text) : GetVerbatim(text).Replace("@", "[at]");
            Message msg = null;

            if (IsChatMessage(text, ref message, ref username) && settings.NormalChatDetection)
            {
                if (!settings.GetIgnoredPlayers().Contains(username))
                {
                    msg = cache.Add(new Message(username,
                        settings.CheckUUID ? settings.GetUUIDDirectlyFromMojang ? sAPI.GetUUIDFromMojang(username) : sAPI.GetUUIDFromPlayerList(username, GetOnlinePlayersWithUUID()) : "00000000000000000000000000000000",
                        message,
                        DateTime.Now));
                }

            }
            else if (IsPrivateMessage(text, ref message, ref username) && settings.SendPrivateMsg)
            {
                if (!settings.GetIgnoredPlayers().Contains(username))
                {
                    msg = cache.Add(new Message(username,
                   settings.CheckUUID ? settings.GetUUIDDirectlyFromMojang ? sAPI.GetUUIDFromMojang(username) : sAPI.GetUUIDFromPlayerList(username, GetOnlinePlayersWithUUID()) : "00000000000000000000000000000000",
                   message,
                   DateTime.Now));
                }
            }
            else if (text.Contains(":") && settings.CustomChatDetection) // Some servers have strange chat formats.
            {
                var messageArray = text.Split(new[] { ':' }, 2);
                if (!settings.GetIgnoredPlayers().Contains(messageArray[0]))
                {
                    msg = cache.Add(new Message(messageArray[0],
                   settings.CheckUUID ? settings.GetUUIDDirectlyFromMojang ? sAPI.GetUUIDFromMojang(messageArray[0]) : sAPI.GetUUIDFromPlayerList(messageArray[0], GetOnlinePlayersWithUUID()) : "00000000000000000000000000000000",
                   messageArray[1],
                   DateTime.Now));
                }
            }
            else if (settings.SendServerMsg)
            {
                msg = cache.Add(new Message("[Server]",
                  "",
                  text,
                  DateTime.Now));

            }
            if (msg == null)
            {
                LogDebugToConsole("Saved message to cache.");
            }
            else { SendWebhook(msg); }
        }
    }

    /// <summary>
    /// Appends pings to the discord message content, if
    /// given keywords are included.
    /// </summary>
    /// <param name="username">Minecraft IGN</param>
    /// <param name="msg">Message content</param>
    /// <returns>Message plus appended pings</returns>
    public string AddPingsToMessage(string username, string msg)
    {
        string pings = "";
        if (settings.GetMessageContains().Count > 0)
        {
            msg = new string(msg.Where(c => !char.IsPunctuation(c)).ToArray()).ToLower();

            foreach (KeyValuePair<string, string> keyPhrase in settings.GetMessageContains())
            {
                if (msg.Contains(keyPhrase.Key)) { pings += string.Join(" ", keyPhrase.Value); }
            }
        }
        if (settings.GetMessageFrom().ContainsKey(username.ToLower()))
        {
            pings += settings.GetMessageFrom()[username.ToLower()];
        }
        return pings;
    }

    /// <summary>
    /// Sends the message to the discord webhook.
    /// </summary>
    /// <param name="msg">Message that will be sent.</param>
    public void SendWebhook(Message msg)
    {
        msg.Content += " " + AddPingsToMessage(msg.SenderName, msg.Content);

        if (settings.WebhookURL != "" && settings.WebhookURL != "https://discord.com/api/webhooks/SomeNumber/RandomStuff")
        {
            LogDebugToConsole("Send webhook request to Discord.");
            try
            {
                HTTP.Post(settings.WebhookURL, new NameValueCollection()
                    {
                        {
                            "username",
                            msg.SenderName
                        },
                        {
                            "content",
                            msg.Content
                        },
                        {
                            "avatar_url",
                            msg.SenderName == "[Server]" ? sAPI.GetSkinURLCrafatar("f78a4d8dd51b4b3998a3230f2de0c670") : sAPI.GetSkinURLCrafatar(msg.SenderUUID)
                        }
                    }
                );
            }
            catch (Exception e)
            {
                LogToConsole("An error occured while posting messages to Discord! (Enable Debug to view it.)");
                LogDebugToConsole(string.Format("Requested Link {0}; Username {1}; message: {2}; error: {3}",
                    msg.SenderName == "[Server]" ? sAPI.GetSkinURLCrafatar("f78a4d8dd51b4b3998a3230f2de0c670") : sAPI.GetSkinURLCrafatar(msg.SenderUUID), msg.SenderName, msg.Content, e.ToString()));
            }
        }
        else
        {
            LogToConsole("No webhook link provided. Please enter one with '/discordwebhook changeurl [link]'");
        }
    }

    /// <summary>
    /// Help Page
    /// </summary>
    /// <returns>Help Page</returns>
    public string GetHelp()
    {
        return "/discordWebhook or /dw 'avatar', 'secondstosaveincache', " +
            "'ping', 'uuidfrommojang'," +
            " 'normalchatdetection', 'customchatdetection'," +
            " 'toggleignored', 'checkuuid'," +
            " 'sendprivate', 'changeurl'," +
            " 'togglesending', 'allowmentions'," +
            " 'onlyprivate', 'help'," +
            " 'getsettings', 'quit'";
    }

    /// <summary>
    /// Returns an array of strings, which are in quotes.
    /// "\"Hello\" \"There\"" => ["Hello", "There"]
    /// </summary>
    /// <param name="rawData">Raw string</param>
    /// <returns>Array with words in quotes.</returns>
    public List<string> GetStringsInQuotes(string rawData)
    {
        List<string> result = new List<string> { "" };
        int currentResultPos = 0;
        bool startCopy = false;

        foreach (char c in rawData)
        {
            if (c == '\"')
            {
                if (startCopy) { result[currentResultPos] = result[currentResultPos].Replace("\"", ""); currentResultPos++; result.Add(""); }
                startCopy = !startCopy;
            }
            if (startCopy)
            {
                result[currentResultPos] += c.ToString();
            }
        }

        return result;
    }

    /// <summary>
    /// Handles all commands.
    /// </summary>
    /// <param name="cmd">Whole command</param>
    /// <param name="args">Only arguments</param>
    /// <returns></returns>
    public string CommandHandler(string cmd, string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "avatar":
                    if (args.Length > 1)
                    {
                        if (args[1] == "size")
                        {

                            try
                            {
                                settings.Size = int.Parse(args[2]);
                                return "Changed avatarsize to " + args[2] + " pixel.";
                            }
                            catch (Exception)
                            {
                                return "That was not a number.";
                            }
                        }

                        if (args[1] == "scale")
                        {
                            try
                            {
                                settings.Scale = int.Parse(args[2]);
                                return "Changed scale to " + args[2] + ".";
                            }
                            catch (Exception)
                            {
                                return "That was not a number.";
                            }
                        }

                        if (args[1] == "fallbackskin")
                        {
                            settings.FallbackSkin = settings.FallbackSkin == "MHF_Steve" ? "MHF_Alex" : "MHF_Steve";
                            return "Changed fallback skin to: " + settings.FallbackSkin;
                        }

                        if (args[1] == "overlay")
                        {
                            settings.Overlay = !settings.Overlay;
                            return "Changed the overlay to: " + settings.Overlay;
                        }

                        if (args[1] == "skintype")
                        {
                            if (args.Length > 2)
                            {
                                if (settings.GetSkinModes().ContainsKey(args[2]))
                                {
                                    settings.CurrentSkinMode = args[2];
                                    return "Changed skin mode to " + args[2];
                                }
                                else
                                {
                                    return "This mode does not exsist. ('flatFace', 'cubeHead', 'fullSkin')";
                                }
                            }
                            else
                            {
                                return "Enter a value! ('flatFace', 'cubeHead', 'fullSkin')";
                            }
                        }
                    }
                    return "This was not a valid option! Try '/dw avatar size/scale/skintype [value]' or '/dw avatar overlay/fallbackskin'.";

                case "secondstosaveincache":
                    try
                    {
                        settings.SecondsToSaveInCache = int.Parse(args[1]);
                        return "Changed the maximum time for a message in cache to " + args[1] + " seconds.";
                    }
                    catch (Exception)
                    {
                        return "That was not a number.";
                    }

                case "ping":
                    if (args.Length > 1)
                    {
                        if (args[1] == "message")
                        {
                            if (args[2] == "add")
                            {
                                List<string> tempList = GetStringsInQuotes(string.Join(" ", args));
                                if (tempList.Count >= 2)
                                {
                                    if (!settings.GetMessageContains().ContainsKey(tempList[0].ToLower()))
                                    {
                                        settings.GetMessageContains().Add(tempList[0].ToLower(), string.Join(" ", tempList[1]));
                                        return "Added " + tempList[0].ToLower() + " " + string.Join(" ", tempList[1]);
                                    }
                                    else
                                    {
                                        return "This ping already exists";
                                    }
                                }
                                else
                                {
                                    return "Too many arguments";
                                }
                            }
                            else
                            {
                                List<string> tempList = GetStringsInQuotes(string.Join(" ", args));
                                if (settings.GetMessageContains().ContainsKey(tempList[0].ToLower()))
                                {
                                    settings.GetMessageContains().Remove(tempList[0].ToLower());
                                    return "Removed " + tempList[0].ToLower();
                                }
                                else
                                {
                                    return "This key does not exsist.";
                                }
                            }
                        }
                        if (args[1] == "sender")
                        {
                            if (args[2] == "add")
                            {
                                List<string> tempList = GetStringsInQuotes(string.Join(" ", args));
                                if (tempList.Count >= 2)
                                {
                                    if (!settings.GetMessageFrom().ContainsKey(tempList[0].ToLower()))
                                    {
                                        settings.GetMessageFrom().Add(tempList[0].ToLower(), string.Join(" ", tempList[1]));
                                        return "Added " + tempList[0].ToLower() + " " + string.Join(" ", tempList[1]);
                                    }
                                    else
                                    {
                                        return "This ping already exists";
                                    }
                                }
                                else
                                {
                                    return "Too many arguments";
                                }
                            }
                            else
                            {
                                List<string> tempList = GetStringsInQuotes(string.Join(" ", args));
                                if (settings.GetMessageFrom().ContainsKey(tempList[0].ToLower()))
                                {
                                    settings.GetMessageFrom().Remove(tempList[0].ToLower());
                                    return "Removed " + tempList[0].ToLower();
                                }
                                else
                                {
                                    return "This key does not exsist.";
                                }
                            }
                        }
                        else
                        {
                            return "This is not a valid option. /discordwebhook ping message/sender add/remove \"Keywords in message\" \"@here <@DiscordID> <@&RoleID>\"";
                        }
                    }
                    else { return "This is not a valid option. /discordwebhook ping message/sender add/remove \"Keywords in message\" \"@here <@DiscordID> <@&RoleID>\""; }

                case "uuidfrommojang":
                    settings.GetUUIDDirectlyFromMojang = !settings.GetUUIDDirectlyFromMojang;
                    return "Getting UUID's from Mojang: " + settings.GetUUIDDirectlyFromMojang.ToString();

                case "checkuuid":
                    settings.CheckUUID = !settings.CheckUUID;
                    return "Getting UUID's: " + settings.CheckUUID.ToString();

                case "sendprivate":
                    settings.SendPrivateMsg = !settings.SendPrivateMsg;
                    return "Send private messages: " + settings.SendPrivateMsg.ToString();

                case "allowmentions":
                    settings.AllowMentions = !settings.AllowMentions;
                    return "People can @Members: " + settings.AllowMentions.ToString();

                case "normalchatdetection":
                    settings.NormalChatDetection = !settings.NormalChatDetection;
                    return "Detect messages with the regular chat detection: " + settings.NormalChatDetection.ToString();

                case "customchatdetection":
                    settings.CustomChatDetection = !settings.CustomChatDetection;
                    return "Detect messages with the custom chat detection: " + settings.CustomChatDetection.ToString();

                case "sendservermsg":
                    settings.SendServerMsg = !settings.SendServerMsg;
                    return "Server messages get forewarded: " + settings.SendServerMsg.ToString();

                case "togglesending":
                    settings.Togglesending = !settings.Togglesending;
                    return "Forewarding messages to Discord: " + settings.Togglesending.ToString();

                case "toggleignored":
                    if (args.Length >= 2)
                    {
                        if (settings.GetIgnoredPlayers().Contains(args[1]))
                        {
                            settings.GetIgnoredPlayers().Remove(args[1]);
                            return "Unignored: " + args[1];
                        }
                        else
                        {
                            settings.GetIgnoredPlayers().Add(args[1]);
                            return "Ignored: " + args[1];
                        }
                    }
                    else { return "Enter a playername."; }

                case "changeurl":
                    if (args.Length > 1)
                    {
                        settings.WebhookURL = args[1];
                        return "Changed webhook URL to: " + args[1];
                    }
                    else
                    {
                        return "Enter a valid Discord Webhook link.";
                    }

                case "getsettings":
                    return "WebhookURL: " + settings.WebhookURL + "\r\n" +
                        "SecondsToSaveInCache: " + settings.SecondsToSaveInCache.ToString() + "\r\n" +
                        "SendPrivateMsg: " + settings.SendPrivateMsg.ToString() + "\r\n" +
                        "SendPublicMsg: " + settings.CustomChatDetection.ToString() + "\r\n" +
                        "SendServerMsg: " + settings.SendServerMsg.ToString() + "\r\n" +
                        "GetUUIDDirectlyFromMojang: " + settings.GetUUIDDirectlyFromMojang.ToString() + "\r\n" +
                        "ToggleSending: " + settings.Togglesending.ToString() + "\r\n" +
                        "AllowMentions: " + settings.AllowMentions.ToString() + "\r\n" +
                        "NormalChatDetection: " + settings.NormalChatDetection.ToString() + "\r\n" +
                        "CustomChatDetection: " + settings.CustomChatDetection.ToString() + "\r\n" +
                        "CurrentSkinMode: " + settings.CurrentSkinMode + "\r\n" +
                        "Size: " + settings.Size.ToString() + "\r\n" +
                        "Scale: " + settings.Scale.ToString() + "\r\n" +
                        "Overlay: " + settings.Overlay.ToString() + "\r\n" +
                        "CheckUUID: " + settings.CheckUUID.ToString() + "\r\n" +
                        "IgnoredPlayers: " + string.Join(" ;", settings.GetIgnoredPlayers()) + "\r\n" +
                        "FallbackSkin: " + settings.FallbackSkin;

                case "help":
                    return GetHelp();

                case "quit":
                    UnloadBot();
                    return "Turned discordwebhook off!";

                default:
                    return GetHelp();
            }
        }
        else { return GetHelp(); }
    }
}
