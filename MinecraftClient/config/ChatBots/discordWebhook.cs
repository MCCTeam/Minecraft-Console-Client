//MCCScript 1.0
//using System.Collections.Specialized;

MCC.LoadBot(new DiscordWebhook());

//MCCScript Extensions
class WebhoookSettings
{
    /// <summary>
    /// All variables for the main class.
    /// </summary>
    public string WebhookURL { get; set; }
    public bool SendPrivateMsg { get; set; }
    public bool SendPublicMsg { get; set; }
    public bool SendServerMsg { get; set; }
    public bool GetUUIDDirectlyFromMojang { get; set; }
    public bool Togglesending { get; set; }
    public bool AllowMentions { get; set; }
    private Dictionary<string, List<string>> messageCache = new Dictionary<string, List<string>>();
    private Dictionary<string, string> messageContains = new Dictionary<string, string>();
    private Dictionary<string, string> messageFrom = new Dictionary<string, string>();

    /// <summary>
    /// All variables for the API class
    /// </summary>

    private string currentSkinMode;
    private int size;
    private int scale;
    private bool overlay;
    private string fallbackSkin;

    private Dictionary<string, string> skinModes = new Dictionary<string, string>();
    public string CurrentSkinMode
    {
        set { if (skinModes.ContainsKey(value)) { currentSkinMode = value; } }
        get { return currentSkinMode; }
        //get; set;
    }
    public int Size
    {
        set { if (value <= 512) { size = value; } }
        get { return size; }
        //get; set;
    }
    public int Scale
    {
        set { if (value <= 10) { this.scale = value; } }
        get { return scale; }
        //get; set;
    }
    public bool Overlay
    {
        set { overlay = value; }
        get { return overlay; }
        //get; set;
    }
    public string FallbackSkin
    {
        set { if (value == "MHF_Steve" || value == "MHF_Alex") { fallbackSkin = value; } }
        get { return fallbackSkin; }
        //get; set;
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
        SendPrivateMsg = true;
        SendPublicMsg = true;
        SendServerMsg = true;
        GetUUIDDirectlyFromMojang = false;
        Togglesending = true;
        AllowMentions = false;
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
        foreach (string key in playerList.Keys)
        {
            if (name.ToLower().Contains(playerList[key].ToLower()))
            {
                return key.Replace("-", "");
            }
        }
        //return playerList.FirstOrDefault(x => name.Contains(x.Value)).Key.Replace("-", "");

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
        if (settings.Togglesending)
        {
            string message = "";
            string username = "";
            username = GetVerbatim(username);
            text = settings.AllowMentions ? GetVerbatim(text) : GetVerbatim(text).Replace("@", "[at]");

            if (IsChatMessage(text, ref message, ref username) && settings.SendPublicMsg)
            {
                SendWebhook(username, message);
            }
            else if (IsPrivateMessage(text, ref message, ref username) && settings.SendPrivateMsg)
            {
                SendWebhook(username, "[Private Message]: " + message);
            }
            else if (text.Contains(":")) // Some servers have strange chat formats.
            {
                var messageArray = text.Split(new[] { ':' }, 2);
                SendWebhook(messageArray[0], messageArray[1]);
            }
            else if (settings.SendPublicMsg)
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

        if (settings.WebhookURL != "" && settings.WebhookURL != null)
        {
            try
            {
                HTTP.Post(settings.WebhookURL, new NameValueCollection()
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
                            username == "[Server]" ? sAPI.GetSkinURLCrafatar("f78a4d8dd51b4b3998a3230f2de0c670") : sAPI.GetSkinURLCrafatar(settings.GetUUIDDirectlyFromMojang ? sAPI.GetUUIDFromMojang(username) : sAPI.GetUUIDFromPlayerList(username, GetOnlinePlayersWithUUID()))
                        }
                    }
                        );
            }
            catch (Exception e)
            {
                /// Mostly exception due to too many requests.
                LogToConsole("An error occured while posting messages to Discord! (Enable Debug to view it.)");
                LogToConsole(string.Format("Requested Link {0}; Username {1}; message: {2}; error: {3}",
                    username == "[Server]" ?
                    sAPI.GetSkinURLCrafatar("f78a4d8dd51b4b3998a3230f2de0c670") : sAPI.GetSkinURLCrafatar(settings.GetUUIDDirectlyFromMojang ?
                    sAPI.GetUUIDFromMojang(username) : sAPI.GetUUIDFromPlayerList(username, GetOnlinePlayersWithUUID())), username, msg, e.ToString()));
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
                        settings.Size = int.Parse(args[1]);
                        return "Changed headsize to " + args[1] + " pixel.";
                    }
                    catch (Exception)
                    {
                        return "That was not a number.";
                    }

                case "scale":
                    try
                    {
                        settings.Scale = int.Parse(args[1]);
                        return "Changed scale to " + args[1] + ".";
                    }
                    catch (Exception)
                    {
                        return "That was not a number.";
                    }


                case "fallbackskin":
                    settings.FallbackSkin = settings.FallbackSkin == "MHF_Steve" ? "MHF_Alex" : "MHF_Steve";
                    return "Changed fallback skin to: " + settings.FallbackSkin;

                case "overlay":
                    settings.Overlay = !settings.Overlay;
                    return "Changed the overlay to: " + settings.Overlay;

                case "skintype":
                    if (args.Length > 1)
                    {
                        if (settings.GetSkinModes().ContainsKey(args[1]))
                        {
                            settings.CurrentSkinMode = args[1];
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
                    settings.GetUUIDDirectlyFromMojang = !settings.GetUUIDDirectlyFromMojang;
                    return "Getting UUID's from Mojang: " + settings.GetUUIDDirectlyFromMojang.ToString();

                case "sendprivate":
                    settings.SendPrivateMsg = !settings.SendPrivateMsg;
                    return "Send private messages: " + settings.SendPrivateMsg.ToString();

                case "allowmentions":
                    settings.AllowMentions = !settings.AllowMentions;
                    return "People can @Members: " + settings.AllowMentions.ToString();


                case "sendservermsg":
                    settings.SendServerMsg = !settings.SendServerMsg;
                    return "Server messages get forewarded: " + settings.SendServerMsg.ToString();

                case "togglesending":
                    settings.Togglesending = !settings.Togglesending;
                    return "Forewarding messages to Discord: " + settings.Togglesending.ToString();

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

                case "help":
                    return GetHelp();

                default:
                    return GetHelp();
            }
        }
        else { return GetHelp(); }
    }
}
