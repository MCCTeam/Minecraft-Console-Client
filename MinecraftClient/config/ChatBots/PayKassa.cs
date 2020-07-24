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
	
    public enum SystemID
    {
        Payeer = 1,
        PerfectMoney = 2,
        AdvCash = 4,
        Berty = 7,
        BitCoin = 11,
        Ethereum = 12,
        LiteCoin = 14,
        DogeCoin = 15,
        Dash = 16,
        BitcoinCash = 18,
        Zcash = 19,
        EthereumClassic = 21,
        Ripple = 22,
        TRON = 27,
        Stellar = 28,

    }

    public string[] sci_create_order(float ammount, string currency, string order_id, string comment, SystemID systemID)
    {
        using (WebClient web = new WebClient())
        {
            string url = "https://paykassa.pro/sci/0.4/index.php?func=sci_create_order&sci_id" + _merchant_id + "&sci_id=" + _merchant_id + "&sci_key=" + _merchant_password + "&order_id=" + order_id + "&amount=" + ammount + "&currency=" + currency + "&system=" + (int)systemID + "&comment=" + comment;
            string response = web.DownloadString(url);
            string urlresponse = Regex.Match(response, "{\"url\":\"(.*)\",\"method\"").Groups[1].Value;
            urlresponse = urlresponse.Replace(@"\/", "/");
            string hash = Regex.Match(response, "\"hash\":\"(.*)\"}}}").Groups[1].Value;
            return new string[] { urlresponse, hash };
        }
    }
	
    public string[] sci_confirm_order(string private_hash)
    {
        using (WebClient web = new WebClient())
        {
            string url = "https://paykassa.pro/sci/0.4/index.php?func=sci_confirm_order&sci_id" + _merchant_id + "&sci_id=" + _merchant_id + "&sci_key=" + _merchant_password + "&private_hash=" + private_hash + "&test=true";
            string response = web.DownloadString(url);
            Console.WriteLine(response);
            string error =Regex.Match(response, "\"error\":(.*),\"message\"").Groups[1].Value;

            string message = Regex.Match(response, "\"message\":\"(.*)\",\"data\"").Groups[1].Value;

            string transaction = Regex.Match(response, "\"transaction\":\"(.*)\",").Groups[1].Value;
            string shop_id = Regex.Match(response, "\"shop_id\":\"(.*)\",").Groups[1].Value;
            string order_id = Regex.Match(response, "\"order_id\":\"(.*)\",").Groups[1].Value;
            string amount = Regex.Match(response, "\"amount\":\"(.*)\",").Groups[1].Value;
            string currency = Regex.Match(response, "\"currency\":\"(.*)\",").Groups[1].Value;
            string system = Regex.Match(response, "\"system\":\"(.*)\",").Groups[1].Value;
            string hash = Regex.Match(response, "\"hash\":\"(.*)\",").Groups[1].Value;
            return new string[] { error.ToString(), message, transaction, shop_id, order_id, amount, currency, system, hash };
        }
    }
}
