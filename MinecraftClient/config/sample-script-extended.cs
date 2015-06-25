//MCCScript 1.0

/* This script demonstrates how to use methods and arguments */

string text = "hello";

if (args.Length > 0)
    text = args[0];
   
for (int i = 0; i < 5; i++)
{
    int count = GetVarAsInt("test") + 1;
    SetVar("test", count);
    SendHelloWorld(count, text);
    SleepBetweenSends();
}

//MCCScript Extensions

/* Here you can define methods for use into your script */

void SendHelloWorld(int count, string text)
{
    /* Warning: Do not make more than one server-related call into a method
     * defined as a script extension eg SendText or switching servers,
     * as execution flow is not managed in the Extensions section */

    SendText("Hello World no. " + count + ": " + text);
}

void SleepBetweenSends()
{
    LogToConsole("Sleeping for 5 seconds...");
    Thread.Sleep(5000);
}