//MCCScript 1.0

MCC.LoadBot(new QIWI_DonationBot());

//MCCScript Extensions

public class QIWI_DonationBot : ChatBot
{	
	public override void Initialize()
    {
		Donation donation = new Donation("token", OnDonate);
		LogToConsole("Бот запужен");
	}
	
	private void OnDonate(string nickname, float ammount, string currency, string message)
    {
        LogToConsole("Ник: " + nickname);
        LogToConsole("Сумма: " + ammount);
        LogToConsole("Валюта: " + currency);
        LogToConsole("Сообщение: " + message);
    }
		
	public override void Update()
	{
		
	}
	public override void AfterGameJoined()
	{

	}
	public override void GetText(string text, string json)
    {
		string text1 = GetVerbatim(text);
	}
}

public class Donation
{
    private string token;
    private List<Action<string, double, string, string>> onDonation { get; set; }
    public Donation(string token, List<Action<string, double, string, string>> onDonation)
    {
        this.token = token;
        this.onDonation = onDonation;
        StartAsync();
    }
    public Donation(string token, Action<string, double, string, string> onDonation)
    {
        this.token = token;
        this.onDonation = new List<Action<string, double, string, string>>();
        this.onDonation.Add(onDonation);
        StartAsync();
    }
    private void StartAsync()
    {
        ServicePointManager.Expect100Continue = true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    string response = wc.DownloadString("https://donate.qiwi.com/api/stream/v1/widgets/" + token + "/events?&limit=1");

                    string type = Regex.Match(response, "\"type\":\"(.*)\",\"status\":\"(.*)\"").Groups[1].Value;
                    if (type == "DONATION")
                    {
                        string nickname = Regex.Match(response, "\"DONATION_SENDER\":\"(.*)\",\"DONATION_AMOUNT\"").Groups[1].Value;
                        string ammountst = Regex.Match(response, "\"DONATION_AMOUNT\":(.*)},\"voteResults\"").Groups[1].Value;
                        double ammount = 0;
                        if (ammountst != "")
                        {
                            ammountst = ammountst.Replace(".", ",").Replace(" ", "");
                            ammount = double.Parse(ammountst);
                        }
                        string currency = Regex.Match(response, "\"DONATION_CURRENCY\":\"(.*)\",\"DONATION_SENDER\"").Groups[1].Value;
                        string message = Regex.Match(response, "\"DONATION_MESSAGE\":\"(.*)\",\"DONATION_CURRENCY\":\"(.*)\"").Groups[1].Value;
                        if (nickname != "")
                        {
                            foreach (Action<string, double, string, string> action in onDonation)
                            {
                                action(nickname, ammount, currency, message);
                            }
                        }
                    }
                }
            }
        });
    }
}
