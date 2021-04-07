//MCCScript 1.0
//using System.Collections.Specialized;

MCC.LoadBot(new DiscordWebhook());

//MCCScript Extensions
class SkinAPI
{
    private Dictionary<string, string> skinModes = new Dictionary<string, string>();
    private string currentSkinMode;
    private int size;
    private int scale;
    private bool overlay;
    private string fallbackSkin;

    public SkinAPI(string newCurrentSkinMode = "flatFace", string newFallbackSkin = "MHF_Steve", int newSize = 512, int newScale = 10, bool newOverlay = true)
    {
        skinModes.Add("flatFace", "https://crafatar.com/avatars/{0}");
        skinModes.Add("cubeHead", "https://crafatar.com/renders/head/{0}");
        skinModes.Add("fullSkin", "https://crafatar.com/renders/body/{0}");

        currentSkinMode = newCurrentSkinMode;
        fallbackSkin = newFallbackSkin;
        size = newSize;
        scale = newScale;
        overlay = newOverlay;
    }

    // Always get a an Answer to a valid name!
    public string getUUIDFromMojang(string name)
    {
        WebClient wc = new WebClient();
        return Json.ParseJson(wc.DownloadString("https://api.mojang.com/users/profiles/minecraft/" + name)).Properties["id"].StringValue;
    }

    // May fail on non premium Servers!
    public string getUUIDFromPlayerList(string name, Dictionary<string, string> playerList)
    {
        return playerList.FirstOrDefault(x => x.Value == name).Key.Replace("-", "");
    }

    public string getSkinURL(string UUID)
    {
        string parameters = string.Join("&", "size=" + size, "scale=" + scale, "default=" + fallbackSkin, (overlay ? "overlay" : ""));
        return string.Format(skinModes[currentSkinMode], UUID + "?" + parameters);
    }

    public void setSize(int newSize) { if (newSize <= 512) { size = newSize; } }
    public void setScale(int newScale) { if (newScale <= 10) { scale = newScale; } }
    public void setCurrentSkinMode(string newSkinMode) { currentSkinMode = newSkinMode; }
    public void toggleOverlay() { overlay = !overlay; }
    public void toggleFallbackSkin() { fallbackSkin = (fallbackSkin == "MHF_Steve" ? "MHF_Alex" : "MHF_Steve"); }

    public int getSize() { return size; }
    public int getScale() { return scale; }
    public bool getOverlay() { return overlay; }
    public string getFallbackSkin() { return fallbackSkin; }
    public Dictionary<string, string> getModeDict() { return skinModes; }
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
    private string webhookURL;
    private bool sendPrivate;
    private bool sendServer;
    private bool getUUIDFromMojang;
    private bool togglesending;
    private bool allowMentions;
    private bool onlyPrivate;
    private Dictionary<string, string> messageContains = new Dictionary<string, string>();
    private Dictionary<string, string> messageFrom = new Dictionary<string, string>();
    SkinAPI sAPI = new SkinAPI();

    public DiscordWebhook()
    {
        getUUIDFromMojang = true;
        sendServer = true;
        togglesending = false;
        allowMentions = false;
        onlyPrivate = false;
    }

    public override void Initialize()
    {
        base.Initialize();
        LogToConsole("Made by Daenges.\nThank you to Crafatar for providing the beautiful avatars!");
        LogToConsole("Please set a Webhook with '/dw changeurl [URL]' and activate the Bot with '/dw pausesending'. For further information type '/dw help'.");
        RegisterChatBotCommand("discordWebhook", "/DiscordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype'", getHelp(), commandHandler);
        RegisterChatBotCommand("dw", "/DiscordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype'", getHelp(), commandHandler);
    }

    public override void GetText(string text)
    {
        if (togglesending)
        {
            string message = "";
            string username = "";
            text = allowMentions ? GetVerbatim(text) : GetVerbatim(text).Replace("@", "[at]");

            if (IsChatMessage(text, ref message, ref username) && !onlyPrivate)
            {
                sendWebhook(username, message);
            }
            else if (IsPrivateMessage(text, ref message, ref username) && sendPrivate)
            {
                sendWebhook(username, "[Private Message]: " + message);
            }
            else if (sendServer)
            {
                sendWebhook("[Server]", text);
            }
        }
    }

    public void setWebhook(string newWebhook) { webhookURL = newWebhook; }

    public void setSkinAPI(string newCurrentSkinType = "flatFace", string newFallbackSkin = "MHF_Steve", int newSize = 512, int newScale = 10, bool newOverlay = true)
    {
        sAPI = new SkinAPI(newCurrentSkinType, newFallbackSkin, newSize, newScale, newOverlay);
    }

    public string addPingsToMessage(string username, string msg)
    {
        string pings = "";
        foreach (string word in msg.Split(' '))
        {
            if (messageContains.ContainsKey(word.ToLower())) { pings += string.Join(" ", messageContains[word.ToLower()]); }
        }
        if (messageFrom.ContainsKey(username.ToLower()))
        {
            pings += messageFrom[username.ToLower()];
        }
        return pings;
    }

    public void sendWebhook(string username, string msg)
    {
        msg += " " + addPingsToMessage(username, msg);

        if (webhookURL != "" && webhookURL != null)
        {
            HTTP.Post(webhookURL, new NameValueCollection()
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
                        username == "[Server]" ? "https://headdb.org/img/renders/852252f1-184f-32ce-ae9a-e1a633878cb3.png" : sAPI.getSkinURL(getUUIDFromMojang ? sAPI.getUUIDFromMojang(username) : sAPI.getUUIDFromPlayerList(username, GetOnlinePlayersWithUUID()))
                    }
                }
                    );
        }
        else
        {
            LogToConsole("No webhook link provided. Please enter one with '/discordwebhook changeurl [link]'");
        }
    }

    public string getHelp()
    {
        return "/discordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype', 'uuidfrommojang', 'sendprivate', 'changeurl', 'togglesending', 'allowmentions', 'onlyprivate', 'help'";
    }

    public List<string> getStringsInQuotes(string rawData)
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

    public string commandHandler(string cmd, string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "size":

                    try
                    {
                        sAPI.setSize(int.Parse(args[1]));
                        return "Changed headsize to " + args[1] + " pixel.";
                    }
                    catch (Exception)
                    {
                        return "That was not a number.";
                    }

                case "scale":
                    try
                    {
                        sAPI.setScale(int.Parse(args[1]));
                        return "Changed scale to " + args[1] + ".";
                    }
                    catch (Exception)
                    {
                        return "That was not a number.";
                    }


                case "fallbackskin":
                    sAPI.toggleFallbackSkin();
                    return "Changed fallback skin to: " + sAPI.getFallbackSkin();

                case "overlay":
                    sAPI.toggleOverlay();
                    return "Changed the overlay to: " + sAPI.getOverlay();

                case "skintype":
                    if (args.Length > 1)
                    {
                        if (sAPI.getModeDict().ContainsKey(args[1]))
                        {
                            sAPI.setCurrentSkinMode(args[1]);
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
                            List<string> tempList = getStringsInQuotes(string.Join(" ", args));
                            if (tempList.Count >= 2)
                            {
                                messageContains.Add(tempList[0].ToLower(), string.Join(" ", tempList[1]));
                                return "Added " + tempList[0].ToLower() + " " + string.Join(" ", tempList[1]);
                            }
                            else
                            {
                                return "Too many arguments";
                            }

                        }
                        else
                        {
                            List<string> tempList = getStringsInQuotes(string.Join(" ", args));
                            if (messageContains.ContainsKey(tempList[0].ToLower()))
                            {
                                messageContains.Remove(tempList[0].ToLower());
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
                            List<string> tempList = getStringsInQuotes(string.Join(" ", args));
                            if (tempList.Count >= 2)
                            {
                                messageFrom.Add(tempList[0].ToLower(), string.Join(" ", tempList[1]));
                                return "Added " + tempList[0].ToLower() + " " + string.Join(" ", tempList[1]);
                            }
                            else
                            {
                                return "Too many arguments";
                            }

                        }
                        else
                        {
                            List<string> tempList = getStringsInQuotes(string.Join(" ", args));
                            if (messageFrom.ContainsKey(tempList[0].ToLower()))
                            {
                                messageFrom.Remove(tempList[0].ToLower());
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
                    getUUIDFromMojang = !getUUIDFromMojang;
                    return "Getting UUID's from Mojang: " + getUUIDFromMojang.ToString();

                case "sendprivate":
                    sendPrivate = !sendPrivate;
                    return "Send private messages: " + sendPrivate.ToString();

                case "allowmentions":
                    allowMentions = !allowMentions;
                    return "People can @Members: " + allowMentions.ToString();

                case "onlyprivate":
                    onlyPrivate = !onlyPrivate;
                    return "Only private messages are sent: " + onlyPrivate.ToString();

                case "sendservermsg":
                    sendServer = !sendServer;
                    return "Server messages get forewarded: " + sendServer.ToString();

                case "togglesending":
                    togglesending = !togglesending;
                    return "Forewarding messages to Discord: " + togglesending.ToString();

                case "changeurl":
                    if (args.Length > 1)
                    {
                        setWebhook(args[1]);
                        return "Changed webhook URL to: " + args[1];
                    }
                    else
                    {
                        return "Enter a valid Discord Webhook link.";
                    }

                case "help":
                    return getHelp();

                default:
                    return getHelp();
            }
        }
        else { return getHelp(); }
    }
}
