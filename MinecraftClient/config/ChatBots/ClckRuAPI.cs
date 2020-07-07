//MCCScript 1.0
//using System.Threading.Tasks;

MCC.LoadBot(new ClckRuAPIBot());

//MCCScript Extensions

public class ClckRuAPIBot : ChatBot
{
    private PayKassaSCI clckapi { get; set; }
    
    public ClckRuAPIBot()
    {
        clckapi = new ClckRuAPI();
    }
	
}

internal class ClckRuAPI
 {
    public string ToCutURl(string url)
    {
        
        WebClient webClient = new WebClient();
        string done = webClient.DownloadString("https://clck.ru/--?url=" + url);
        return done;
    }
 }

