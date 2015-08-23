//MCCScript 1.0

/* This is a sample script for Minecraft Console Client
 * The code provided in this file will be compiled at runtime and executed
 * Allowed instructions: Any C# code AND methods provided by the MCC API */

for (int i = 0; i < 5; i++)
{
    int count = MCC.GetVarAsInt("test") + 1;
    MCC.SetVar("test", count);
    MCC.SendText("Hello World no. " + count);
    MCC.LogToConsole("Sleeping for 5 seconds...");
    Thread.Sleep(5000);
}