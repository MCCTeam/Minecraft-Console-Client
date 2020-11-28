//MCCScript 1.0

MCC.LoadBot(new WatchLamp());

//MCCScript Extensions

/* The ChatBot will access the world on a regular basis to watch for a lamp.
 * This is an example of how the world around the player can be accessed from a C# script. */

class WatchLamp : ChatBot
{
    /* == CONFIG == */

    int lampX = 0;
    int lampY = 64;
    int lampZ = 0;

    /* == CODE == */

    int checkCount = 0;
    Location lampLoc;

    public WatchLamp()
    {
        if (!Settings.TerrainAndMovements)
        {
            LogToConsole("WARNING: Terrain handling is disabled in INI file.");
            LogToConsole("WARNING: This means this bot cannot watch for lamps.");
            UnloadBot();
        }
        else
        {
            lampLoc = new Location(lampX, lampY, lampZ);
            LogToConsole("Watching lamp at " + lampLoc);
        }
    }

    public override void Update()
    {
        if (checkCount > 10)
        {
            checkCount = 0;
            Material blockType = GetWorld().GetBlock(lampLoc).Type;
            switch (blockType)
            {
                case Material.RedstoneLampOn:
                    //Lamp is on. All right. Nothing to say here.
                    break;
                case Material.RedstoneLampOff:
                    LogToConsole("Lamp at " + lampLoc + " is currently turned OFF !!!");
                    for (int i = 0; i < 3; i++)
                        Console.Beep();
                    break;
                default:
                    LogToConsole("Block at " + lampLoc + " is not a lamp: " + blockType + "...");
                    break;
            }
        }
        else checkCount++;
    }
}