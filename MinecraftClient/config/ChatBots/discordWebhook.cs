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
        string parameters = string.Join("&", "size=" + size, "scale=" + scale,"default=" + fallbackSkin, (overlay ? "overlay" : ""));
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
    private bool getUUIDFromMojang;
    private bool pauseSending;
    private bool allowMentions;
    private bool onlyPrivate;
    SkinAPI sAPI = new SkinAPI();

    public DiscordWebhook()
    {
        getUUIDFromMojang = true;
        pauseSending = true;
        allowMentions = false;
        onlyPrivate = false;
    }

    public override void Initialize()
    {
        base.Initialize();
        LogToConsole("Made by Daenges.\nThank you to Crafatar for providing the beautiful avatars!");
        LogToConsole("Please set a Webhook with '/discordwebhook changeurl [URL]' and activate the Bot with '/discordwebhook pausesending'. For further information type '/discordwebhook help'.");
        RegisterChatBotCommand("discordWebhook", "/DiscordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype'", getHelp(), commandHandler);
    }

    public override void GetText(string text)
    {
        if (!pauseSending)
        {
            string message = "";
            string username = "";
            text = allowMentions ? GetVerbatim(text) : GetVerbatim(text).Replace("@", "[at]");

            if (IsChatMessage(text, ref message, ref username) && !onlyPrivate)
            {
                sendWebhook(username, message);
            }
            if (IsPrivateMessage(text, ref message, ref username) && sendPrivate)
            {
                sendWebhook(username, "[Private Message]: " + message);
            }
        }
    }

    public void setWebhook(string newWebhook) { webhookURL = newWebhook; }

    public void setSkinAPI(string newCurrentSkinType = "flatFace", string newFallbackSkin = "MHF_Steve", int newSize = 512, int newScale = 10, bool newOverlay = true)
    {
        sAPI = new SkinAPI(newCurrentSkinType, newFallbackSkin, newSize, newScale, newOverlay);
    }

    public void sendWebhook(string username, string msg)
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
                sAPI.getSkinURL(getUUIDFromMojang ? sAPI.getUUIDFromMojang(username) : sAPI.getUUIDFromPlayerList(username, GetOnlinePlayersWithUUID()))
            }
        }
        );
    }

    public string getHelp()
    {
        return "/discordWebhook 'size', 'scale', 'fallbackSkin', 'overlay', 'skintype', 'uuidfrommojang', 'sendprivate', 'changeurl', 'pausesending', 'allowmentions', 'onlyprivate', 'help'";
    }

    public string commandHandler(string cmd, string[] args)
    {
        if (args.Length > 0)
        {
            switch(args[0])
            {
                case "size":

                    try {
                        sAPI.setSize(int.Parse(args[1]));
                        return "Changed headsize to " + args[1] + " pixel.";
                    } catch (Exception)
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
                    } else
                    {
                        return "Enter a value! ('flatFace', 'cubeHead', 'fullSkin')";
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

                case "pausesending":
                    pauseSending = !pauseSending;
                    return "Paused sending messages to Discord: " + pauseSending.ToString();

                case "changeurl":
                    if (args.Length > 1)
                    {
                        setWebhook(args[1]);
                        return "Changed webhook URL to: " + args[1];
                    } else 
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
