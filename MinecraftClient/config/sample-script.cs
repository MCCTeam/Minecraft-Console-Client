//MCCScript 1.0

/* This is a sample script for Minecraft Console Client
 * The code provided in this file will be compiled at runtime and executed
 * Allowed instructions: Any C# code AND all methods provided by the bot API */

for (int i = 0; i < 5; i++)
{
    int count = GetVarAsInt("test");
    count++;
    SetVar("test", count);
    SendText("Hello World no. " + count);
    PerformInternalCommand("log Sleeping for 5 seconds...");
    Thread.Sleep(5000);
}