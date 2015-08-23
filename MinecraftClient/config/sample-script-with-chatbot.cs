//MCCScript 1.0

/* This is a sample script that will load a ChatBot into Minecraft Console Client
 * Simply execute the script once with /script or the script scheduler to load the bot */

MCC.LoadBot(new ExampleBot());

//MCCScript Extensions

/* The ChatBot class must be defined as an extension of the script in the Extensions section
 * The class can override common methods from ChatBot.cs, take a look at MCC's source code */

public class ExampleBot : ChatBot
{
    public override void Initialize()
    {
        LogToConsole("Sucessfully Initialized!");
    }

    public override void GetText(string text)
    {
        string message = "";
        string username = "";
        text = GetVerbatim(text);

        if (IsChatMessage(text, ref message, ref username))
        {
            LogToConsole("Public message from " + username + ": " + message);
        }
        else if (IsPrivateMessage(text, ref message, ref username))
        {
            LogToConsole("Private message from " + username + ": " + message);
        }
    }
}