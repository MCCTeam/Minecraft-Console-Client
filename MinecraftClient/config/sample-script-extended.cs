//MCCScript 1.0

/* This script demonstrates how to use methods and arguments */

string text = "hello";

if (args.Length > 0)
    text = args[0];
   
for (int i = 0; i < 5; i++)
{
    int count = MCC.GetVarAsInt("test") + 1;
    MCC.SetVar("test", count);
    SendHelloWorld(count, text);
    SleepBetweenSends();
}

//MCCScript Extensions

/* Here you can define methods for use into your script */

void SendHelloWorld(int count, string text)
{
    MCC.SendText("Hello World no. " + count + ": " + text);
}

void SleepBetweenSends()
{
    MCC.LogToConsole("Sleeping for 5 seconds...");
    Thread.Sleep(5000);
}