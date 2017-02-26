//MCCScript 1.0

MCC.LoadBot(new PMForwarder());

//MCCScript Extensions

/// <summary>
/// This bot can forward received PMs to other players
/// </summary>
public class PMForwarder : ChatBot
{
    private const string PMRecipientsFile = "pm-forward-to.txt";
    private string[] pmRecipients;

    public PMForwarder()
    {
        pmRecipients = LoadDistinctEntriesFromFile(PMRecipientsFile);
        if (Settings.Bots_Owners.Count == 0)
            LogToConsole("No Bot owners in Settings INI file. Unloading.");
        else if (pmRecipients.Length == 0)
            LogToConsole("No PM Recipients in '" + PMRecipientsFile + "'. Unloading.");
        else LogToConsole(String.Format(
            "Forwarding PMs from owners {0} to recipients {1}",
            String.Join(", ", Settings.Bots_Owners), String.Join(", ", pmRecipients)));
    }

    public override void GetText(string text)
    {
        text = GetVerbatim(text);
        string message = "", sender = "";
        if (IsPrivateMessage(text, ref message, ref sender) && Settings.Bots_Owners.Contains(sender.ToLower().Trim()))
        {
            LogToConsole("Forwarding PM to " + String.Join(", ", pmRecipients));
            foreach (string recipient in pmRecipients)
                SendPrivateMessage(recipient, message);
        }
    }
}
}