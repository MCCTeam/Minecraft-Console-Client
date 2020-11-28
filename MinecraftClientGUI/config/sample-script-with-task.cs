//MCCScript 1.0

MCC.LoadBot(new PeriodicTask());

//MCCScript Extensions

/// <summary>
/// The ChatBot API is not thread-safe so tasks must occur on the main thread.
/// This bot shows an example of running a task periodically without using threads.
/// </summary>
public class PeriodicTask : ChatBot
{
    private DateTime nextTaskRun = DateTime.Now;

    /// <summary>
    /// Called on each MCC tick, around 10 times per second
    /// </summary>
    public override void Update()
    {
        DateTime dateNow = DateTime.Now;
        if (nextTaskRun < dateNow)
        {
            LogDebugToConsole("Running task @ " + dateNow);

            // Your task here
            SendText("/ping");

            // Schedule next run
            nextTaskRun = dateNow.AddSeconds(60);
        }
    }
}