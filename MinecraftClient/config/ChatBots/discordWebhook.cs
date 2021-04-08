//MCCScript 1.0
//using System.Collections.Specialized;

MCC.LoadBot(new DiscordWebhook());

//MCCScript Extensions
class WebhoookSettings
{
    /// <summary>
    /// All variables for the main class.
    /// </summary>
    public string webhookURL { get; set; }
    public bool sendPrivateMsg { get; set; }
    public bool sendPublicMsg { get; set; }
    public bool sendServerMsg { get; set; }
    public bool getUUIDFromMojang { get; set; }
    public bool togglesending { get; set; }
    public bool allowMentions { get; set; }
    private Dictionary<string, List<string>> messageCache = new Dictionary<string, List<string>>();
    private Dictionary<string, string> messageContains = new Dictionary<string, string>();
    private Dictionary<string, string> messageFrom = new Dictionary<string, string>();

    /// <summary>
    /// All variables for the API class
    /// </summary>
    private Dictionary<string, string> skinModes = new Dictionary<string, string>();
    public string currentSkinMode
    {
        //set { if(skinModes.ContainsKey(value)) { this.currentSkinMode = value; } }
        //get { return this.currentSkinMode; }
        get; set;
    }
    public int size
    {
        //set { if (value <= 512) { this.size = value; } }
        //get { return this.size; }
        get; set;
    }
    public int scale
    {
        //set { if (value <= 10) { this.scale = value; } }
        //get { return this.scale; }
        get; set;
    }
    public bool overlay
    {
        //set { this.overlay = value; }
        //get { return this.overlay; }
        get; set;
    }
    public string fallbackSkin
    {
        //set { if (value == "MHF_Steve" || value == "MHF_Alex") { this.fallbackSkin = value; } }
        //get { return this.fallbackSkin; }
        get; set;
    }

    /// <summary>
    /// Setup standard settings.
    /// </summary>
    public WebhoookSettings()
    {
        // Set preconfigured skinModes //
        skinModes.Add("flatFace", "https://crafatar.com/avatars/{0}");
        skinModes.Add("cubeHead", "https://crafatar.com/renders/head/{0}");
        skinModes.Add("fullSkin", "https://crafatar.com/renders/body/{0}");

        // Define standard values for main class //
        sendPrivateMsg = true;
        sendPublicMsg = true;
        sendServerMsg = true;
        getUUIDFromMojang = true;
        togglesending = true;
        allowMentions = false;
        currentSkinMode = "flatFace";

        // Define standard values for API class //
        size = 100;
        scale = 4;
        overlay = true;
        fallbackSkin = "MHF_Steve";
    }

    public Dictionary<string, string> GetMessageContains() { return this.messageContains; }
    public void SetMessageContains(Dictionary<string, string> value) { this.messageContains = value; }

    public Dictionary<string, string> GetMessageFrom() { return this.messageFrom; }
    public void SetMessageFrom(Dictionary<string, string> value) { this.messageFrom = value; }

    public Dictionary<string, List<string>> GetCachedMessages() { return this.messageCache; }
    public void SetCachedMessages(Dictionary<string, List<string>> value) { this.messageCache = value; }

    public Dictionary<string, string> GetSkinModes() { return this.skinModes; }
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
        WebClient wc = new WebClient();
        return Json.ParseJson(wc.DownloadString("https://api.mojang.com/users/profiles/minecraft/" + name)).Properties["id"].StringValue;
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
        return playerList.FirstOrDefault(x => name.Contains(x.Value)).Key.Replace("-", "");
    }

    /// <summary>
    /// Creates the url which is forewarded to the webhook.
    /// </summary>
    /// <param name="UUID"> Player UUID </param>
    /// <returns></returns>
    public string GetSkinURLCrafatar(string UUID)
    {
        string parameters = string.Join("&", "size=" + settings.size, "scale=" + settings.scale, "default=" + settings.fallbackSkin, (settings.overlay ? "overlay" : ""));
        return string.Format(settings.GetSkinModes()[settings.currentSkinMode], UUID + "?" + parameters);
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
    SkinAPI sAPI;

    public DiscordWebhook()
    {
        sAPI = new SkinAPI(settings);
    }

    public override void Initialize()
    {
        LogToConsole("Made by Daenges.\nThank you to Crafatar for providing the beautiful avatars!");
        LogToConsole("Please set a Webhook with '/discordwebhook changeurl [URL]' and activate the Bot with '/discordwebhook pausesending'. For further information type '/discordwebhook help'.");
        RegisterChatBotCommand("discordWebhook", "/DiscordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype'", GetHelp(), CommandHandler);
        RegisterChatBotCommand("dw", "/DiscordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype'", GetHelp(), CommandHandler);
    }

    public override void GetText(string text)
    {
        if (settings.togglesending)
        {
            string message = "";
            string username = "";
            text = settings.allowMentions ? GetVerbatim(text) : GetVerbatim(text).Replace("@", "[at]");

            if (IsChatMessage(text, ref message, ref username) && settings.sendPublicMsg)
            {
                SendWebhook(username, message);
            }
            else if (IsPrivateMessage(text, ref message, ref username) && settings.sendPrivateMsg)
            {
                SendWebhook(username, "[Private Message]: " + message);
            }
            else if (settings.sendPublicMsg)
            {
                SendWebhook("[Server]", text);
            }
        }
    }

    public string AddPingsToMessage(string username, string msg)
    {
        string pings = "";
        foreach (string word in msg.Split(' '))
        {
            if (settings.GetMessageContains().ContainsKey(word.ToLower())) { pings += string.Join(" ", settings.GetMessageContains()[word.ToLower()]); }
        }
        if (settings.GetMessageFrom().ContainsKey(username.ToLower()))
        {
            pings += settings.GetMessageFrom()[username.ToLower()];
        }
        return pings;
    }

    public void SendWebhook(string username, string msg)
    {
        msg += " " + AddPingsToMessage(username, msg);

        if (settings.webhookURL != "" && settings.webhookURL != null)
        {
            try
            {
                HTTP.Post(settings.webhookURL, new NameValueCollection()
                    {
                        {
                            "username",
                            username
                        },
                        {
                            "content",
                            msg
                        },
                        {
                            "avatar_url",
                            username == "[Server]" ? "https://headdb.org/img/renders/852252f1-184f-32ce-ae9a-e1a633878cb3.png" : sAPI.GetSkinURLCrafatar(settings.getUUIDFromMojang ? sAPI.GetUUIDFromMojang(username) : sAPI.GetUUIDFromPlayerList(username, GetOnlinePlayersWithUUID()))
                        }
                    }
                        );
            }
            catch (Exception e)
            {
                /// Mostly exception due to too many requests.
                LogToConsole("An error occured while posting messages to Discord! (Enable Debug to view it.)");
                LogDebugToConsole(e.ToString());
            }
        }
        else
        {
            LogToConsole("No webhook link provided. Please enter one with '/discordwebhook changeurl [link]'");
        }
    }

    public string GetHelp()
    {
        return "/discordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype', 'uuidfrommojang', 'sendprivate', 'changeurl', 'togglesending', 'allowmentions', 'onlyprivate', 'help'";
    }

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

    public string CommandHandler(string cmd, string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "size":

                    try
                    {
                        settings.size = int.Parse(args[1]);
                        return "Changed headsize to " + args[1] + " pixel.";
                    }
                    catch (Exception)
                    {
                        return "That was not a number.";
                    }

                case "scale":
                    try
                    {
                        settings.scale = int.Parse(args[1]);
                        return "Changed scale to " + args[1] + ".";
                    }
                    catch (Exception)
                    {
                        return "That was not a number.";
                    }


                case "fallbackskin":
                    settings.fallbackSkin = settings.fallbackSkin == "MHF_Steve" ? "MHF_Alex" : "MHF_Steve";
                    return "Changed fallback skin to: " + settings.fallbackSkin;

                case "overlay":
                    settings.overlay = !settings.overlay;
                    return "Changed the overlay to: " + settings.overlay;

                case "skintype":
                    if (args.Length > 1)
                    {
                        if (settings.GetSkinModes().ContainsKey(args[1]))
                        {
                            settings.currentSkinMode = args[1];
                            return "Changed skin mode to " + args[1];
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

                case "ping":
                    if (args[1] == "message")
                    {
                        if (args[2] == "add")
                        {
                            List<string> tempList = GetStringsInQuotes(string.Join(" ", args));
                            if (tempList.Count >= 2)
                            {
                                settings.GetMessageContains().Add(tempList[0].ToLower(), string.Join(" ", tempList[1]));
                                return "Added " + tempList[0].ToLower() + " " + string.Join(" ", tempList[1]);
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
                                settings.GetMessageFrom().Add(tempList[0].ToLower(), string.Join(" ", tempList[1]));
                                return "Added " + tempList[0].ToLower() + " " + string.Join(" ", tempList[1]);
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
                        return "This is not a valid option. /discordwebhook ping message/sender add/remove \"Keywords in message\" \"@Pings @To @Append @To @Message\"";
                    }

                case "uuidfrommojang":
                    settings.getUUIDFromMojang = !settings.getUUIDFromMojang;
                    return "Getting UUID's from Mojang: " + settings.getUUIDFromMojang.ToString();

                case "sendprivate":
                    settings.sendPrivateMsg = !settings.sendPrivateMsg;
                    return "Send private messages: " + settings.sendPrivateMsg.ToString();

                case "allowmentions":
                    settings.allowMentions = !settings.allowMentions;
                    return "People can @Members: " + settings.allowMentions.ToString();


                case "sendservermsg":
                    settings.sendServerMsg = !settings.sendServerMsg;
                    return "Server messages get forewarded: " + settings.sendServerMsg.ToString();

                case "togglesending":
                    settings.togglesending = !settings.togglesending;
                    return "Forewarding messages to Discord: " + settings.togglesending.ToString();

                case "changeurl":
                    if (args.Length > 1)
                    {
                        settings.webhookURL = args[1];
                        return "Changed webhook URL to: " + args[1];
                    }
                    else
                    {
                        return "Enter a valid Discord Webhook link.";
                    }

                case "help":
                    return GetHelp();

                default:
                    return GetHelp();
            }
        }
        else { return GetHelp(); }
    }
}
