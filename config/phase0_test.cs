//MCCScript 1.0

MCC.LoadBot(new Phase0Test());

//MCCScript Extensions

public class Phase0Test : ChatBot
{
    private int phase = 0;
    private int ticksInPhase = 0;

    public override void Initialize()
    {
        LogToConsole("=== Phase 0 Physics Test ===");
    }

    public override void AfterGameJoined()
    {
        LogToConsole("Joined. Starting tests...");
    }

    public override void Update()
    {
        ticksInPhase++;

        switch (phase)
        {
            case 0: // Setup area
                if (ticksInPhase == 1)
                {
                    LogToConsole("[Setup] Creating test area at spawn...");
                    SendText("/tp @s 0 80 0");
                }
                if (ticksInPhase == 40)
                    SendText("/fill -5 79 -5 15 79 15 stone");
                if (ticksInPhase == 50)
                    SendText("/fill -5 80 -5 15 85 15 air");
                if (ticksInPhase == 60)
                    SendText("/tp @s 0 80 0");
                if (ticksInPhase >= 80) NextPhase();
                break;

            case 1: // Test crawling: place 1-block-high ceiling
                if (ticksInPhase == 1)
                {
                    LogToConsole("[Test 1] CRAWLING - Placing ceiling at y=81 above player (1 block headroom)");
                    SendText("/setblock 0 81 0 stone");
                }
                if (ticksInPhase == 40)
                {
                    var loc = GetCurrentLocation();
                    LogToConsole("[Test 1] Pos: " + loc + " - Check debug log for Swimming/crawl pose");
                }
                if (ticksInPhase == 80)
                {
                    LogToConsole("[Test 1] Removing ceiling...");
                    SendText("/setblock 0 81 0 air");
                }
                if (ticksInPhase == 100)
                {
                    var loc = GetCurrentLocation();
                    LogToConsole("[Test 1] After removal pos: " + loc + " - Should be back to Standing");
                }
                if (ticksInPhase >= 120) NextPhase();
                break;

            case 2: // Test slime bounce (no sneak)
                if (ticksInPhase == 1)
                {
                    LogToConsole("[Test 2] SLIME BOUNCE - Placing slime blocks and falling");
                    SendText("/fill 8 79 0 10 79 2 slime_block");
                }
                if (ticksInPhase == 20)
                {
                    LogToConsole("[Test 2] Teleporting 10 blocks above slime...");
                    SendText("/tp @s 9 90 1");
                }
                if (ticksInPhase % 10 == 0 && ticksInPhase >= 30 && ticksInPhase <= 100)
                {
                    var loc = GetCurrentLocation();
                    LogToConsole("[Test 2] tick=" + ticksInPhase + " Pos: " + loc);
                }
                if (ticksInPhase >= 160) NextPhase();
                break;

            case 3: // Test sneaking (move with sneak)
                if (ticksInPhase == 1)
                {
                    LogToConsole("[Test 3] SNEAK MOVEMENT");
                    SendText("/tp @s 0 80 0");
                }
                if (ticksInPhase == 30)
                {
                    LogToConsole("[Test 3] Moving to (5,80,0) with unsafe path...");
                    MoveToLocation(new Location(5, 80, 0), allowUnsafe: true);
                }
                if (ticksInPhase % 10 == 0 && ticksInPhase >= 30 && ticksInPhase <= 80)
                {
                    var loc = GetCurrentLocation();
                    LogToConsole("[Test 3] tick=" + ticksInPhase + " Pos: " + loc);
                }
                if (ticksInPhase >= 100) NextPhase();
                break;

            case 4: // Done
                if (ticksInPhase == 1)
                {
                    LogToConsole("=== Phase 0 Tests Complete ===");
                    LogToConsole("Check debug log for [Physics] messages.");
                    UnloadBot();
                }
                break;
        }
    }

    private void NextPhase()
    {
        phase++;
        ticksInPhase = 0;
    }
}
