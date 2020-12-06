//MCCScript 1.0
//using System.Threading.Tasks;

MCC.LoadBot(new QIWI_DonationBot());

//MCCScript Extensions

public class QIWI_DonationBot : ChatBot
{	
        //More info: https://github.com/Nekiplay/QIWI-API
        public override void Initialize()
        {
		QIWI.Donation donation = new QIWI.Donation("token", OnDonate);
                QIWI.Wallet wallet = new QIWI.Wallet("token");
                Console.WriteLine("Phone: " + wallet.Identification.Phone()
                + "\nMail: " + wallet.Identification.Mail()
                + "\nBalance: " + wallet.Balance.RUB(wallet.Identification.Phone()) + " RUB"
                );
                Console.WriteLine("Name: " + wallet.Identification.Last_Name(wallet.Identification.Phone()) + " " + wallet.Identification.First_Name(wallet.Identification.Phone()) + " " + wallet.Identification.Middle_Name(wallet.Identification.Phone()));
		LogToConsole("Bot enabled");
	}
	
        private void OnDonate(string nickname, float ammount, string currency, string message)
        {
        	LogToConsole("Nickname: " + nickname);
        	LogToConsole("Ammount: " + ammount);
        	LogToConsole("Currency: " + currency);
        	LogToConsole("Message: " + message);
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
        public enum Currency
        {
            RUB = 643,
            USD = 840,
        };
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
            public string Nickname(string phone)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com//qw-nicknames/v1/persons/" + phone + "/nickname");
                    string First_Name = Regex.Match(response, "\"nickname\":\"(.*)\",\"canChange\":(.*)").Groups[1].Value;
                    if (First_Name != "")
                    {
                        return First_Name;
                    }
                }
                return "";
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
            public _Transfer_ Transfer;
            
            public _Balance_(string token)
            {
                this.token = token;
                this.Transfer = new _Transfer_(this.token);
            }
            public class _Transfer_
            {
                private string token;
                public _Transfer_(string token)
                {
                    this.token = token;
                }

                public bool QIWIRUB(string phone, double ammount, string comment)
                {
                    Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                    string jsonv2 = "{\"id\":\"" + 1000 * unixTimestamp + "\",\"sum\":{\"amount\":" + ammount + ",\"currency\":\"" + "643" + "\"},\"paymentMethod\":{\"type\":\"Account\",\"accountId\":\"643\"},\"comment\":\"" + comment + "\",\"fields\":{\"account\":\"" + phone + "\"}}";

                    /* Отправка */
                    try
                    {
                        WebRequest request = WebRequest.Create("https://edge.qiwi.com/sinap/api/v2/terms/99/payments");
                        request.Method = "POST";
                        byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(jsonv2);
                        request.ContentType = "application/json";
                        request.Headers["Authorization"] = "Bearer " + this.token;
                        request.ContentLength = byteArray.Length;

                        //записываем данные в поток запроса
                        using (Stream dataStream = request.GetRequestStream())
                        {
                            dataStream.Write(byteArray, 0, byteArray.Length);
                        }

                        WebResponse response = request.GetResponse();
                        using (Stream stream = response.GetResponseStream())
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                reader.ReadToEnd();
                                return true;
                            }
                        }
                        response.Close();
                    } 
                    catch (WebException ex)
                    {
                        return false;
                    }
                    return false;
                }
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
            public double USD(string phone)
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Set("authorization", "Bearer " + this.token);
                    string response = wc.DownloadString("https://edge.qiwi.com/funding-sources/v2/persons/" + phone + "/accounts");
                    //Console.WriteLine(response);
                    string usds = Regex.Match(response, "{\"alias\":\"qw_wallet_usd\",\"fsAlias\":\"qb_wallet\",\"bankAlias\":\"QIWI\",\"title\":\"Qiwi Account\",\"type\":{\"id\":\"WALLET\",\"title\":\"Visa QIWI Wallet\"},\"hasBalance\":(.*),\"balance\":{\"amount\":(.*),\"currency\":840},\"currency\":840,\"defaultAccount\":(.*)}").Groups[2].Value;
                    //Console.WriteLine("Доларров: " + usds);
                    if (usds != "")
                    {
                        usds = usds.Replace(" ", "").Replace(".", ",");
                        //Console.WriteLine("Доларров: " + usds);
                        double usd = double.Parse(usds);
                        return usd;
                    }
                    else
                        return 0.0;
                }
                return 0.0;
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
