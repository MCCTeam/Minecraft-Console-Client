//MCCScript 1.0

/* This script demonstrates how to add fields and methods */

for (int i = 0; i < 5; i++)
{
    int count = GetVarAsInt("test") + 1;
    SetVar("test", count);
    SendHelloWorld(count);
    SleepBetweenSends();
}

//MCCScript Extensions

/* Here you can define methods for use into your script */

void SendHelloWorld(int count)
{
    /* Warning: Do not make more than one server-related call into a method
     * defined as a script extension eg SendText or switching servers,
     * as execution flow is not managed in the Extensions section */

    SendText("Hello World no. " + count);
}

void SleepBetweenSends()
{
    LogToConsole("Sleeping for 5 seconds...");
    Thread.Sleep(5000);
}