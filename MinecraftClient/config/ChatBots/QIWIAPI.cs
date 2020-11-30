//MCCScript 1.0

MCC.LoadBot(new QIWI_DonationBot());

//MCCScript Extensions

public class QIWI_DonationBot : ChatBot
{	
	public override void Initialize()
    {
		QIWI.Donation donation = new QIWI.Donation("token", OnDonate);
        QIWI.Wallet wallet = new QIWI.Wallet("token");
        Console.WriteLine("Номер телефона: " + wallet.Identification.Phone()
            + "\nПочта: " + wallet.Identification.Mail()
            + "\nБаланс: " + wallet.Balance.RUB(wallet.Identification.Phone()) + " RUB"
            );
        Console.WriteLine("Имя: " + wallet.Identification.Last_Name(wallet.Identification.Phone()) + " " + wallet.Identification.First_Name(wallet.Identification.Phone()) + " " + wallet.Identification.Middle_Name(wallet.Identification.Phone()));
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

public class QIWI
{
    public class Wallet
    {
        public _Balance_ Balance;
        public _Identification_ Identification;

        private string token;
        public Wallet(string token)
        {
            this.token = token;
            this.Balance = new _Balance_(this.token);
            this.Identification = new _Identification_(this.token);
        }

        public class _Identification_
        {
            private string token;
            public _Identification_(string token)
            {
                this.token = token;
            }
            public string First_Name(string phone)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com/identification/v1/persons/" + phone + "/identification");
                    string First_Name = Regex.Match(response, "\"firstName\":\"(.*)\",\"middleName\":\"(.*)\",\"lastName\":\"(.*)\"").Groups[1].Value;
                    if (First_Name != "")
                    {
                        return First_Name;
                    }
                }
                return "";
            }
            public string Middle_Name(string phone)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com/identification/v1/persons/" + phone + "/identification");
                    string Middle_Name = Regex.Match(response, "\"firstName\":\"(.*)\",\"middleName\":\"(.*)\",\"lastName\":\"(.*)\"").Groups[2].Value;
                    if (Middle_Name != "")
                    {
                        return Middle_Name;
                    }
                }
                return "";
            }
            public string Last_Name(string phone)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com/identification/v1/persons/" + phone + "/identification");
                    string Last_Name = Regex.Match(response, "\"lastName\":\"(.*)\",\"birthDate\":\"(.*)\"").Groups[1].Value;
                    if (Last_Name != "")
                    {
                        return Last_Name;
                    }
                }
                return "";
            }
            public string Phone()
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com/person-profile/v1/profile/current?authInfoEnabled=true&contractInfoEnabled=true&userInfoEnabled=true");
                    string phone = Regex.Match(response, "\"personId\":(.*),\"registrationDate\":\"(.*)\"").Groups[1].Value;
                    if (phone != "")
                    {
                        return phone;
                    }
                }
                return "";
            }
            public string Mail()
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com/person-profile/v1/profile/current?authInfoEnabled=true&contractInfoEnabled=true&userInfoEnabled=true");
                    string mail = Regex.Match(response, "\"boundEmail\":\"(.*)\",\"emailSettings\"").Groups[1].Value;
                    if (mail != "")
                    {
                        return mail;
                    }
                }
                return "";
            }
        }
        public class _Balance_
        {
            private string token;
            public _Balance_(string token)
            {
                this.token = token;
            }

            public double RUB(string phone)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com/funding-sources/v2/persons/" + phone + "/accounts");
                    string rubles = Regex.Match(response, "\"balance\":{\"amount\":(.*),\"currency\":643}").Groups[1].Value;
                    if (rubles != "")
                    {
                        rubles = rubles.Replace(".", ",").Replace(" ", "");
                        double rub = double.Parse(rubles);
                        return rub;
                    }
                    else
                        return 0.0;
                }
            }
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
}
