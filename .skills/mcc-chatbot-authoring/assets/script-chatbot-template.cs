//MCCScript 1.0

MCC.LoadBot(new ExampleScriptBot());

//MCCScript Extensions

public class ExampleScriptBot : ChatBot
{
    public override void Initialize()
    {
        LogToConsole("ExampleScriptBot initialized.");
    }

    public override void AfterGameJoined()
    {
        // Safe place for startup chat or commands.
    }

    public override void GetText(string text)
    {
        text = GetVerbatim(text);

        string message = "";
        string username = "";

        if (IsPrivateMessage(text, ref message, ref username))
        {
            LogToConsole("PM from " + username + ": " + message);
            return;
        }

        if (IsChatMessage(text, ref message, ref username))
        {
            LogToConsole("Chat from " + username + ": " + message);
        }
    }
}
