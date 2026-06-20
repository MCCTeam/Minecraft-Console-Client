//MCCScript 1.0

MCC.LoadBot(new TickCounterBot());

//MCCScript Extensions

public class TickCounterBot : ChatBot
{
    private const int CaptureDurationSeconds = 5;

    private DateTime _captureEndsAt = DateTime.MaxValue;
    private bool _captureStarted;
    private int _updateCount;

    public override void AfterGameJoined()
    {
        _captureEndsAt = DateTime.UtcNow.AddSeconds(CaptureDurationSeconds);
        _captureStarted = true;
        _updateCount = 0;
        LogToConsole($"Counting MCC update ticks for {CaptureDurationSeconds} seconds.");
    }

    public override void Update()
    {
        if (!_captureStarted)
            return;

        _updateCount++;

        if (DateTime.UtcNow < _captureEndsAt)
            return;

        _captureStarted = false;

        double ticksPerSecond = _updateCount / (double)CaptureDurationSeconds;
        LogToConsole($"Tick counter summary: updates={_updateCount}, seconds={CaptureDurationSeconds}, tps={ticksPerSecond:F2}");

        UnloadBot();
    }
}
