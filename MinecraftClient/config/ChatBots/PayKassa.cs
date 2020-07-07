//MCCScript 1.0
//using System.Threading.Tasks;

//==== CONFIG START ====
string merchant_id = "";
string merchant_password = "";
//====  CONFIG END  ====

MCC.LoadBot(new PayKassaBot(merchant_id, merchant_password));

//MCCScript Extensions

public class PayKassaBot : ChatBot
{
    private PayKassaSCI paykassa { get; set; }

    /// <summary>
    /// This bot forwarding messages between Minecraft and VKonrakte chats.
    /// Shares only messages that starts with dot ("."). Example: .Hello!
    /// Also, send message to VK when any player joins or leaves.
    /// </summary>
    /// <param name="vkToken">VK Community token</param>
    /// <param name="chatId">VK ChatId</param>
    /// <param name="botCommunityId">Bot's CommunityId</param>
    public PayKassaBot(string merchant_id, string merchant_password)
    {
        paykassa = new PayKassaSCI(merchant_id, merchant_password);
    }
	
}

/// <summary>
/// Client for VK Community (bot) LongPool API.
/// Also can send messages.
/// </summary>

internal class PayKassaSCI
{
    private string _merchant_id = "";
    private string _merchant_password = "";
    public PayKassaSCI(string merchant_id, string merchant_password)
    {
        this._merchant_id = merchant_id;
        this._merchant_password = merchant_password;
    }

    public string sci_create_order(float ammount, string currency, string order_id, string comment, string system_id)
    {
        using (WebClient web = new WebClient())
        {
            string url = "https://paykassa.pro/sci/0.4/index.php?func=sci_create_order&sci_id" + _merchant_id + "&sci_id=" + _merchant_id + "&sci_key=" + _merchant_password + "&order_id=" + order_id + "&amount=" + ammount + "&currency=" + currency + "&system=" + system_id + "&comment=" + comment;
            string response = web.DownloadString(url);
            string urlresponse = Regex.Match(response, "{\"url\":\"(.*)\",\"method\"").Groups[1].Value;
            urlresponse = urlresponse.Replace(@"\/", "/");
            if (urlresponse != string.Empty)
            {
                return urlresponse;

            }
            else
            {
                return string.Empty;
            }
        }
    }
}
